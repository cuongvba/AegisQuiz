using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Enterprise Universal Dynamic Math Dictionary Service]
    /// Dịch vụ đăng bạ từ điển toán học động theo kiến trúc Clean Architecture:
    /// 1. Tải và đồng bộ hóa từ điển mở rộng từ JSON (math_dictionary_registry.json).
    /// 2. Lưu trữ trong ConcurrentDictionary tốc độ cao O(1) phục vụ hàng triệu request.
    /// 3. Cung cấp cơ chế Hot-Reload 0ms downtime qua API hoặc File System.
    /// 4. Hỗ trợ Singleton Instance tĩnh cho các Engine lõi (WMF, OMML, DocxParser).
    /// </summary>
    public class DynamicMathDictionaryService : IDynamicMathDictionaryService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly ILogger<DynamicMathDictionaryService>? _logger;

        // Lưu trữ dữ liệu bộ nhớ đệm tốc độ cao O(1)
        private readonly ConcurrentDictionary<string, string> _symbolGlyphs = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string> _symbolLatex = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string> _greekLetters = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, string> _greekLatex = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, string> _ommlTokens = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, string> _accentMappings = new(StringComparer.Ordinal);
        private readonly List<(Regex Regex, string Replacement)> _inlineNormalizations = new();
        private readonly List<ICompiledStructuralMatcher> _structuralMatchers = new();
        private readonly object _lock = new();

        private MathDictionaryRegistryDto _currentRegistry = new();
        private string? _registryFilePath;

        /// <summary>
        /// Singleton Instance tĩnh toàn cục phục vụ các Static Engine v8.5
        /// </summary>
        public static IDynamicMathDictionaryService Instance { get; internal set; } = new DynamicMathDictionaryService();

        public DynamicMathDictionaryService(ILogger<DynamicMathDictionaryService>? logger = null)
        {
            _logger = logger;
            InitializeRegistry();
        }

        private void InitializeRegistry()
        {
            try
            {
                _registryFilePath = ResolveRegistryFilePath();
                if (!string.IsNullOrEmpty(_registryFilePath) && File.Exists(_registryFilePath))
                {
                    string json = File.ReadAllText(_registryFilePath);
                    HotReload(json);
                    _logger?.LogInformation("[MathDictionary] Successfully loaded dynamic math registry from: {Path} (Tokens: {TokensCount}, Glyphs: {GlyphsCount})",
                        _registryFilePath, _ommlTokens.Count, _symbolGlyphs.Count);
                }
                else
                {
                    _logger?.LogWarning("[MathDictionary] Registry file not found. Initialized with empty dynamic cache; hardcoded Tier-3 fallbacks will be used.");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MathDictionary] Error initializing dynamic math registry from file. Fallback to hardcoded tables.");
            }
        }

        private static string? ResolveRegistryFilePath()
        {
            var probeDirs = new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory };
            foreach (var startDir in probeDirs)
            {
                if (string.IsNullOrWhiteSpace(startDir)) continue;
                var dir = new DirectoryInfo(startDir);
                while (dir != null && dir.Exists)
                {
                    string c1 = Path.Combine(dir.FullName, "src", "AegisQuiz.Infrastructure", "Config", "math_dictionary_registry.json");
                    if (File.Exists(c1)) return c1;

                    string c2 = Path.Combine(dir.FullName, "Backend", "src", "AegisQuiz.Infrastructure", "Config", "math_dictionary_registry.json");
                    if (File.Exists(c2)) return c2;

                    string c3 = Path.Combine(dir.FullName, "Config", "math_dictionary_registry.json");
                    if (File.Exists(c3)) return c3;

                    dir = dir.Parent;
                }
            }

            return null;
        }

        private void EnsureInitialized()
        {
            if (_symbolLatex.IsEmpty || _symbolGlyphs.IsEmpty)
            {
                lock (_lock)
                {
                    if (_symbolLatex.IsEmpty || _symbolGlyphs.IsEmpty)
                    {
                        InitializeRegistry();
                    }
                }
            }
        }

        public string ResolveSymbolGlyph(string font, char c, string defaultFallback)
        {
            EnsureInitialized();
            int charCode = (int)c;
            string fontLower = (font ?? "symbol").Trim().ToLowerInvariant();

            // 1. Thử khóa chính xác font:charCode (VD: "symbol:162" hoặc "euclid symbol:162")
            string key1 = $"{fontLower}:{charCode}";
            if (_symbolGlyphs.TryGetValue(key1, out var val1))
            {
                return val1;
            }

            // 2. Thử dạng hex font:0x00A2
            string keyHex = $"{fontLower}:0x{charCode:x4}";
            if (_symbolGlyphs.TryGetValue(keyHex, out var valHex))
            {
                return valHex;
            }

            // 3. Thử với font "symbol" mặc định nếu font chứa chữ "symbol"
            bool isSymbolFont = fontLower.Contains("symbol") || fontLower.Contains("extra");
            if (isSymbolFont)
            {
                if (_symbolGlyphs.TryGetValue($"symbol:{charCode}", out var valSym))
                {
                    return valSym;
                }
                if (_symbolGlyphs.TryGetValue($"symbol:0x{charCode:x4}", out var valSymHex))
                {
                    return valSymHex;
                }
                // 4. Chỉ với font Symbol mới cho phép tra mã charCode đơn thuần
                if (_symbolGlyphs.TryGetValue(charCode.ToString(), out var valCode))
                {
                    return valCode;
                }
            }
            else
            {
                if (charCode >= 128 && _symbolGlyphs.TryGetValue(charCode.ToString(), out var valCode))
                {
                    return valCode;
                }
            }

            return defaultFallback;
        }

        public string ResolveSymbolLatex(string font, char c, string defaultFallback)
        {
            EnsureInitialized();
            int charCode = (int)c;
            string fontLower = (font ?? "symbol").Trim().ToLowerInvariant();

            // 1. Thử khóa chính xác font:charCode (VD: "symbol:162" hoặc "euclid symbol:162")
            string key1 = $"{fontLower}:{charCode}";
            if (_symbolLatex.TryGetValue(key1, out var val1))
            {
                return val1;
            }

            // 2. Thử dạng hex font:0x00A2
            string keyHex = $"{fontLower}:0x{charCode:x4}";
            if (_symbolLatex.TryGetValue(keyHex, out var valHex))
            {
                return valHex;
            }

            // 3. Thử với font "symbol" mặc định nếu font chứa chữ "symbol"
            bool isSymbolFont = fontLower.Contains("symbol") || fontLower.Contains("extra");
            if (isSymbolFont)
            {
                if (_symbolLatex.TryGetValue($"symbol:{charCode}", out var valSym))
                {
                    return valSym;
                }
                if (_symbolLatex.TryGetValue($"symbol:0x{charCode:x4}", out var valSymHex))
                {
                    return valSymHex;
                }
                // 4. Chỉ với font Symbol mới cho phép tra mã charCode đơn thuần
                if (_symbolLatex.TryGetValue(charCode.ToString(), out var valCode))
                {
                    return valCode;
                }
            }
            else
            {
                // Với font Latin thông thường (Times New Roman, Arial...), chỉ cho phép tra charCode nếu >= 128 (ký tự đặc biệt)
                // Tuyệt đối không tra cứu các chữ cái Latin ASCII (như 'D'=68) sang bảng ký hiệu Hy Lạp (\Delta)
                if (charCode >= 128 && _symbolLatex.TryGetValue(charCode.ToString(), out var valCode))
                {
                    return valCode;
                }
            }

            return defaultFallback;
        }

        public string ResolveGreekLetter(string font, char c, string defaultFallback)
        {
            EnsureInitialized();
            string fontLower = (font ?? "symbol").Trim().ToLowerInvariant();

            // 1. Khóa theo font và ký tự
            string key = $"{fontLower}:{c}";
            if (_greekLetters.TryGetValue(key, out var val))
            {
                return val;
            }

            // 2. Khóa font symbol chuẩn
            if (_greekLetters.TryGetValue($"symbol:{c}", out var valSym))
            {
                return valSym;
            }

            // 3. Khóa theo ký tự thuần
            if (_greekLetters.TryGetValue(c.ToString(), out var valChar))
            {
                return valChar;
            }

            return defaultFallback;
        }

        public string ResolveGreekLatex(string font, char c, string defaultFallback)
        {
            string fontLower = (font ?? "symbol").Trim().ToLowerInvariant();

            // 1. Khóa theo font và ký tự
            string key = $"{fontLower}:{c}";
            if (_greekLatex.TryGetValue(key, out var val))
            {
                return val;
            }

            // 2. Khóa font symbol chuẩn
            if (_greekLatex.TryGetValue($"symbol:{c}", out var valSym))
            {
                return valSym;
            }

            // 3. Khóa theo ký tự thuần
            if (_greekLatex.TryGetValue(c.ToString(), out var valChar))
            {
                return valChar;
            }

            return defaultFallback;
        }

        public string ResolveOmmlToken(string token, string defaultFallback)
        {
            if (string.IsNullOrEmpty(token)) return defaultFallback;

            if (_ommlTokens.TryGetValue(token, out var latex))
            {
                return latex;
            }

            return defaultFallback;
        }

        public string ResolveAccent(string accentChar, string baseContent)
        {
            if (string.IsNullOrEmpty(accentChar))
            {
                return baseContent.Length > 2 ? $"\\overrightarrow{{{baseContent}}}" : $"\\vec{{{baseContent}}}";
            }

            if (_accentMappings.TryGetValue(accentChar, out var template))
            {
                if (template.Contains("{0}"))
                {
                    return string.Format(template, baseContent);
                }
                if (template.Contains("{base}"))
                {
                    return template.Replace("{base}", baseContent);
                }
                if (template is "'" or "''")
                {
                    return $"{{{baseContent}}}{template}";
                }
                if (template == "\\vec" && baseContent.Length > 2)
                {
                    return $"\\overrightarrow{{{baseContent}}}";
                }
                if (template.StartsWith("\\"))
                {
                    return $"{template}{{{baseContent}}}";
                }
                return $"{template}{{{baseContent}}}";
            }

            return string.Empty;
        }

        public string NormalizeInlineText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            lock (_lock)
            {
                foreach (var (regex, replacement) in _inlineNormalizations)
                {
                    text = regex.Replace(text, replacement);
                }
            }

            return text;
        }

        public bool HotReload(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                var dto = JsonSerializer.Deserialize<MathDictionaryRegistryDto>(json, JsonOptions);
                if (dto == null) return false;

                lock (_lock)
                {
                    _symbolGlyphs.Clear();
                    _symbolLatex.Clear();
                    _greekLetters.Clear();
                    _greekLatex.Clear();
                    _ommlTokens.Clear();
                    _accentMappings.Clear();
                    _inlineNormalizations.Clear();
                    _structuralMatchers.Clear();

                    // Nạp Symbol Glyphs
                    if (dto.SymbolGlyphs != null)
                    {
                        foreach (var entry in dto.SymbolGlyphs)
                        {
                            RegisterSymbolGlyphInternal(entry);
                        }
                    }

                    // Nạp Greek Letters
                    if (dto.GreekLetters != null)
                    {
                        foreach (var entry in dto.GreekLetters)
                        {
                            RegisterGreekLetterInternal(entry);
                        }
                    }

                    // Nạp OMML Tokens
                    if (dto.OmmlTokens != null)
                    {
                        foreach (var kvp in dto.OmmlTokens)
                        {
                            _ommlTokens[kvp.Key] = kvp.Value;
                        }
                    }

                    // Nạp Accent Mappings
                    if (dto.AccentMappings != null)
                    {
                        foreach (var kvp in dto.AccentMappings)
                        {
                            _accentMappings[kvp.Key] = kvp.Value;
                        }
                    }

                    // Nạp Inline Normalization Rules
                    if (dto.InlineNormalizations != null)
                    {
                        foreach (var rule in dto.InlineNormalizations)
                        {
                            if (!string.IsNullOrEmpty(rule.Pattern))
                            {
                                try
                                {
                                    var regex = new Regex(rule.Pattern, RegexOptions.Compiled);
                                    _inlineNormalizations.Add((regex, rule.Replacement ?? string.Empty));
                                }
                                catch (Exception ex)
                                {
                                    _logger?.LogWarning(ex, "[MathDictionary] Invalid regex pattern in inline normalization: {Pattern}", rule.Pattern);
                                }
                            }
                        }
                    }

                    // Nạp Structural Simplification Rules (Declarative WMF -> KaTeX)
                    if (dto.StructuralSimplificationRules != null)
                    {
                        foreach (var rule in dto.StructuralSimplificationRules.Where(r => r.Enabled).OrderByDescending(r => r.Priority))
                        {
                            try
                            {
                                _structuralMatchers.Add(new CompiledStructuralMatcher(rule));
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogWarning(ex, "[MathDictionary] Failed to compile structural rule: {RuleId}", rule.Id);
                            }
                        }
                    }

                    dto.LastUpdated = DateTime.UtcNow;
                    _currentRegistry = dto;
                }

                _logger?.LogInformation("[MathDictionary] Hot-reload successfully applied. Total OMML tokens: {Omml}, Glyphs: {Glyphs}, LatexGlyphs: {LatexGlyphs}, StructuralRules: {Rules}",
                    _ommlTokens.Count, _symbolGlyphs.Count, _symbolLatex.Count, _structuralMatchers.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MathDictionary] Failed to deserialize or apply hot-reload JSON.");
                return false;
            }
        }

        private void RegisterSymbolGlyphInternal(SymbolGlyphEntryDto entry)
        {
            if (string.IsNullOrEmpty(entry.Replacement) && string.IsNullOrEmpty(entry.Latex)) return;

            string fontLower = (entry.Font ?? "symbol").Trim().ToLowerInvariant();
            string hexClean = (entry.CharHex ?? "").Trim();

            int charCode = -1;
            if (!string.IsNullOrEmpty(hexClean))
            {
                try
                {
                    string num = hexClean.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? hexClean[2..]
                        : hexClean;
                    charCode = Convert.ToInt32(num, 16);
                }
                catch
                {
                    // ignore invalid hex format
                }
            }

            if (!string.IsNullOrEmpty(entry.Replacement))
            {
                if (charCode >= 0)
                {
                    _symbolGlyphs[$"{fontLower}:{charCode}"] = entry.Replacement;
                    _symbolGlyphs[$"{fontLower}:0x{charCode:x4}"] = entry.Replacement;
                    if (charCode >= 128)
                    {
                        _symbolGlyphs[charCode.ToString()] = entry.Replacement;
                    }
                }

                if (!string.IsNullOrEmpty(hexClean))
                {
                    _symbolGlyphs[$"{fontLower}:{hexClean.ToLowerInvariant()}"] = entry.Replacement;
                }
            }

            if (!string.IsNullOrWhiteSpace(entry.Latex))
            {
                if (charCode >= 0)
                {
                    _symbolLatex[$"{fontLower}:{charCode}"] = entry.Latex;
                    _symbolLatex[$"{fontLower}:0x{charCode:x4}"] = entry.Latex;
                    if (charCode >= 128)
                    {
                        _symbolLatex[charCode.ToString()] = entry.Latex;
                    }
                }

                if (!string.IsNullOrEmpty(hexClean))
                {
                    _symbolLatex[$"{fontLower}:{hexClean.ToLowerInvariant()}"] = entry.Latex;
                }
            }
        }

        private void RegisterGreekLetterInternal(GreekLetterEntryDto entry)
        {
            if (string.IsNullOrEmpty(entry.Char)) return;

            string fontLower = (entry.Font ?? "symbol").Trim().ToLowerInvariant();

            if (!string.IsNullOrEmpty(entry.Replacement))
            {
                _greekLetters[$"{fontLower}:{entry.Char}"] = entry.Replacement;
                _greekLetters[entry.Char] = entry.Replacement;
            }

            if (!string.IsNullOrWhiteSpace(entry.Latex))
            {
                _greekLatex[$"{fontLower}:{entry.Char}"] = entry.Latex;
                _greekLatex[entry.Char] = entry.Latex;
            }
        }

        public MathDictionaryRegistryDto GetCurrentRegistry()
        {
            lock (_lock)
            {
                return new MathDictionaryRegistryDto
                {
                    Version = _currentRegistry.Version,
                    LastUpdated = _currentRegistry.LastUpdated,
                    SymbolGlyphs = new List<SymbolGlyphEntryDto>(_currentRegistry.SymbolGlyphs),
                    GreekLetters = new List<GreekLetterEntryDto>(_currentRegistry.GreekLetters),
                    OmmlTokens = new Dictionary<string, string>(_currentRegistry.OmmlTokens, StringComparer.Ordinal),
                    AccentMappings = new Dictionary<string, string>(_currentRegistry.AccentMappings, StringComparer.Ordinal),
                    InlineNormalizations = new List<InlineNormalizationRuleDto>(_currentRegistry.InlineNormalizations),
                    StructuralSimplificationRules = new List<MathStructuralRuleDto>(_currentRegistry.StructuralSimplificationRules),
                    EngineSettings = _currentRegistry.EngineSettings ?? new MathEngineTypographyDto()
                };
            }
        }

        public MathEngineTypographyDto GetEngineSettings()
        {
            lock (_lock)
            {
                return _currentRegistry.EngineSettings ?? new MathEngineTypographyDto();
            }
        }

        public void UpdateEngineSettings(MathEngineTypographyDto settings)
        {
            if (settings == null) return;
            lock (_lock)
            {
                _currentRegistry.EngineSettings = settings;
                _currentRegistry.LastUpdated = DateTime.UtcNow;
                try
                {
                    if (!string.IsNullOrEmpty(_registryFilePath))
                    {
                        string updatedJson = JsonSerializer.Serialize(_currentRegistry, JsonOptions);
                        File.WriteAllText(_registryFilePath, updatedJson);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "[MathDictionary] Could not persist engine settings to disk registry file.");
                }
            }
        }

        public SubjectMathProfileDto GetSubjectProfile(string? subjectCode)
        {
            var settings = GetEngineSettings();
            if (!string.IsNullOrWhiteSpace(subjectCode) &&
                settings.SubjectProfiles != null &&
                settings.SubjectProfiles.TryGetValue(subjectCode.Trim(), out var profile))
            {
                return profile;
            }

            return new SubjectMathProfileDto
            {
                SubjectCode = "DEFAULT",
                SubjectName = "Tiêu Chuẩn",
                BaseFontUnit = settings.BaseFontUnit,
                LetterSpacing = settings.LetterSpacing,
                MinInlineHeight = settings.MinInlineHeight,
                CropPadX = settings.CropPadX,
                CropPadY = settings.CropPadY
            };
        }

        public void UpsertSymbolGlyph(SymbolGlyphEntryDto entry)
        {
            if (entry == null) return;
            lock (_lock)
            {
                RegisterSymbolGlyphInternal(entry);
                var existing = _currentRegistry.SymbolGlyphs
                    .FirstOrDefault(x => string.Equals(x.Font, entry.Font, StringComparison.OrdinalIgnoreCase) &&
                                         string.Equals(x.CharHex, entry.CharHex, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.Replacement = entry.Replacement;
                    existing.Latex = entry.Latex;
                    existing.Desc = entry.Desc;
                }
                else
                {
                    _currentRegistry.SymbolGlyphs.Add(entry);
                }
                _currentRegistry.LastUpdated = DateTime.UtcNow;
            }
        }

        public void UpsertOmmlToken(string token, string latex)
        {
            if (string.IsNullOrEmpty(token)) return;
            lock (_lock)
            {
                _ommlTokens[token] = latex;
                _currentRegistry.OmmlTokens[token] = latex;
                _currentRegistry.LastUpdated = DateTime.UtcNow;
            }
        }

        public bool TryMatchStructuralRule(
            IReadOnlyList<(int x1, int y1, int x2, int y2)> lines,
            List<WmfSimpleTextItem> textItems,
            int polyCount,
            int complexShapes,
            int baseFs,
            out List<WmfSimpleTextItem> modifiedItems)
        {
            modifiedItems = textItems;
            if (textItems == null || textItems.Count == 0) return false;

            List<ICompiledStructuralMatcher> matchers;
            lock (_lock)
            {
                matchers = _structuralMatchers.ToList();
            }

            if (matchers.Count == 0) return false;

            var currentItems = textItems;
            bool anyMatched = false;

            foreach (var matcher in matchers)
            {
                if (matcher.TryMatch(lines, currentItems, polyCount, complexShapes, baseFs, out var nextItems))
                {
                    currentItems = nextItems;
                    anyMatched = true;
                }
            }

            if (anyMatched)
            {
                modifiedItems = currentItems;
                return true;
            }

            return false;
        }

        public void UpsertStructuralRule(MathStructuralRuleDto rule)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.Id)) return;
            lock (_lock)
            {
                var existingIndex = _currentRegistry.StructuralSimplificationRules.FindIndex(r => string.Equals(r.Id, rule.Id, StringComparison.OrdinalIgnoreCase));
                if (existingIndex >= 0)
                {
                    _currentRegistry.StructuralSimplificationRules[existingIndex] = rule;
                }
                else
                {
                    _currentRegistry.StructuralSimplificationRules.Add(rule);
                }

                // Tái biên dịch các matcher theo độ ưu tiên giảm dần
                _structuralMatchers.Clear();
                foreach (var r in _currentRegistry.StructuralSimplificationRules.Where(r => r.Enabled).OrderByDescending(r => r.Priority))
                {
                    _structuralMatchers.Add(new CompiledStructuralMatcher(r));
                }

                _currentRegistry.LastUpdated = DateTime.UtcNow;
                PersistRegistryToFile();
            }
        }

        public bool DeleteStructuralRule(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId)) return false;
            lock (_lock)
            {
                int count = _currentRegistry.StructuralSimplificationRules.RemoveAll(r => string.Equals(r.Id, ruleId, StringComparison.OrdinalIgnoreCase));
                if (count > 0)
                {
                    _structuralMatchers.Clear();
                    foreach (var r in _currentRegistry.StructuralSimplificationRules.Where(r => r.Enabled).OrderByDescending(r => r.Priority))
                    {
                        _structuralMatchers.Add(new CompiledStructuralMatcher(r));
                    }
                    _currentRegistry.LastUpdated = DateTime.UtcNow;
                    PersistRegistryToFile();
                    return true;
                }
                return false;
            }
        }

        private void PersistRegistryToFile()
        {
            try
            {
                if (!string.IsNullOrEmpty(_registryFilePath))
                {
                    string updatedJson = JsonSerializer.Serialize(_currentRegistry, JsonOptions);
                    File.WriteAllText(_registryFilePath, updatedJson);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[MathDictionary] Could not persist registry changes to disk.");
            }
        }
    }
}
