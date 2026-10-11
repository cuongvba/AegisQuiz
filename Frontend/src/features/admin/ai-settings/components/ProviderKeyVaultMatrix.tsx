import React from 'react';
import { Server } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { SecureKeyVaultEditor } from './SecureKeyVaultEditor';

export interface ProviderConfig {
  providerType: number;
  displayName: string;
  baseUrl: string;
  apiKeys: string[];
  defaultModel: string;
  isEnabled: boolean;
  priority: number;
  costPerMillionInputTokens: number;
  costPerMillionOutputTokens: number;
}

interface ProviderKeyVaultMatrixProps {
  providers: ProviderConfig[];
  onToggleEnabled: (providerType: number) => void;
  onUpdateKeys: (providerType: number, keys: string[]) => void;
}

export const ProviderKeyVaultMatrix: React.FC<ProviderKeyVaultMatrixProps> = ({
  providers,
  onToggleEnabled,
  onUpdateKeys,
}) => {
  const { t } = useTranslation('ai_settings');

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-bold text-white flex items-center gap-2">
          <Server className="w-5 h-5 text-indigo-400" />
          {t('key_vault.title')}
        </h2>
        <span className="text-xs text-slate-400">
          {t('key_vault.round_robin_hint')}
        </span>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
        {providers.map(p => (
          <div 
            key={p.providerType}
            className={`p-5 rounded-2xl border transition-all ${
              p.isEnabled 
                ? 'bg-slate-900/90 border-slate-800 hover:border-indigo-500/40 shadow-lg' 
                : 'bg-slate-950/40 border-slate-800/50 opacity-60'
            }`}
          >
            {/* Header của Provider Card */}
            <div className="flex items-center justify-between pb-3 border-b border-slate-800">
              <div className="flex items-center gap-2.5">
                <div 
                  className={`w-3 h-3 rounded-full ${
                    p.isEnabled 
                      ? 'bg-emerald-400 shadow-md shadow-emerald-400/50' 
                      : 'bg-slate-600'
                  }`} 
                />
                <div>
                  <h3 className="font-bold text-white text-base">{p.displayName}</h3>
                  <p className="text-xs text-slate-400 font-mono">{p.defaultModel}</p>
                </div>
              </div>

              <div className="flex items-center gap-3">
                <span className="text-[11px] font-mono px-2 py-0.5 rounded bg-slate-800 text-slate-300">
                  {t('key_vault.priority_badge', { priority: p.priority })}
                </span>
                
                {/* Switch bật/tắt provider */}
                <label className="relative inline-flex items-center cursor-pointer">
                  <input 
                    type="checkbox" 
                    checked={p.isEnabled} 
                    onChange={() => onToggleEnabled(p.providerType)}
                    className="sr-only peer"
                  />
                  <div className="w-11 h-6 bg-slate-700 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-indigo-600"></div>
                </label>
              </div>
            </div>

            {/* Bể Khóa Bảo Mật (Secure Masked Vault) */}
            <div className="pt-4">
              <SecureKeyVaultEditor
                providerType={p.providerType}
                providerName={p.displayName}
                apiKeys={p.apiKeys}
                onChange={(newKeys) => onUpdateKeys(p.providerType, newKeys)}
              />

              {/* Thông tin chi phí Token ước tính */}
              <div className="flex items-center justify-between text-[11px] text-slate-500 pt-3 border-t border-slate-800/60 mt-3">
                <span>
                  {t('key_vault.cost_summary', {
                    inputCost: p.costPerMillionInputTokens,
                    outputCost: p.costPerMillionOutputTokens,
                  })}
                </span>
                <span className="font-mono text-slate-600 truncate max-w-[200px]" title={p.baseUrl}>
                  {p.baseUrl}
                </span>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
