import React, { useState, useEffect, useRef } from 'react';
import { 
  FileText, Brain, Sparkles, BookOpen, Layers, ListTodo, Loader2, 
  UploadCloud, Trash2, ArrowUpRight, CheckCircle2, AlertCircle, RefreshCw,
  Plus, Bot, Shield, Sliders
} from 'lucide-react';
import { notebookService, NotebookSummaryDto } from '../../services/notebook.service';
import { NotebookDualPaneStudio } from './notebooks/NotebookDualPaneStudio';

export function AdminNotebooksPage() {
  const [notebooks, setNotebooks] = useState<NotebookSummaryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [progress, setProgress] = useState(0);
  const [statusMessage, setStatusMessage] = useState('');
  
  // Studio Modal state
  const [selectedNotebookId, setSelectedNotebookId] = useState<string | null>(null);
  const [studioOpen, setStudioOpen] = useState(false);

  // Create Hub Modal state
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [hubTitle, setHubTitle] = useState('');
  const [hubDescription, setHubDescription] = useState('');
  const [hubDomain, setHubDomain] = useState('GENERAL');
  const [hubPersonaRole, setHubPersonaRole] = useState(1);
  const [creatingHub, setCreatingHub] = useState(false);

  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    loadNotebooks();
  }, []);

  const loadNotebooks = async () => {
    setLoading(true);
    try {
      const data = await notebookService.getNotebooks();
      setNotebooks(data);
    } catch (err) {
      console.error('Lỗi khi nạp danh sách notebooks:', err);
    } finally {
      setLoading(false);
    }
  };

  const handlePickFile = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    await processUpload(file);
    e.target.value = '';
  };

  const processUpload = async (file: File) => {
    setUploading(true);
    setProgress(15);
    setStatusMessage(`Đang tải lên và phân tích tài liệu ${file.name}...`);

    try {
      const res = await notebookService.uploadNotebook(file, {}, (percent) => {
        const scaled = Math.min(60, Math.max(15, Math.round(percent * 0.6)));
        setProgress(scaled);
        if (scaled >= 50) {
          setStatusMessage('AI đang tái cấu trúc không gian & bóc tách các phân đoạn nhận thức...');
        }
      });

      setProgress(85);
      setStatusMessage('Đang phân tích Bloom Taxonomy & hiệu chỉnh mô hình IRT 3-PL...');
      
      setTimeout(async () => {
        setProgress(100);
        setStatusMessage('✅ Tạo Không Gian Tri Thức thành công!');
        await loadNotebooks();
        setUploading(false);
        if (res?.id) {
          setSelectedNotebookId(res.id);
          setStudioOpen(true);
        }
      }, 800);

    } catch (err: any) {
      console.error('Lỗi upload file:', err);
      setStatusMessage('❌ Lỗi xử lý tài liệu. Vui lòng thử lại.');
      setTimeout(() => setUploading(false), 2000);
    }
  };

  const handleCreateHub = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!hubTitle.trim()) return;

    setCreatingHub(true);
    try {
      const hub = await notebookService.createHub({
        title: hubTitle.trim(),
        description: hubDescription.trim() || undefined,
        domainCode: hubDomain,
        personaRole: hubPersonaRole
      });

      setShowCreateModal(false);
      setHubTitle('');
      setHubDescription('');
      await loadNotebooks();

      if (hub?.id) {
        setSelectedNotebookId(hub.id);
        setStudioOpen(true);
      }
    } catch (err) {
      console.error('Lỗi khởi tạo Không Gian Tri Thức Hub:', err);
    } finally {
      setCreatingHub(false);
    }
  };

  const handleDelete = async (e: React.MouseEvent, id: string) => {
    e.stopPropagation();
    if (!window.confirm('Bạn có chắc chắn muốn xóa không gian tri thức này khỏi hệ thống?')) return;
    try {
      await notebookService.deleteNotebook(id);
      setNotebooks((prev) => prev.filter((nb) => nb.id !== id));
    } catch (err) {
      console.error('Lỗi xóa notebook:', err);
    }
  };

  const openStudio = (id: string) => {
    setSelectedNotebookId(id);
    setStudioOpen(true);
  };

  return (
    <div className="space-y-8 max-w-7xl mx-auto pb-12">
      {/* HEADER */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-black text-transparent bg-clip-text bg-gradient-to-r from-blue-400 via-purple-400 to-indigo-300 tracking-tight">
            AegisNotebook Sovereign V2.0
          </h1>
          <p className="text-sm text-slate-400 mt-1 font-medium">
            Hồ Sơ Nhận Thức Sống Đa Nguồn & Nhà Máy Khảo Thí Tự Động Đỉnh Cao Hàng Đầu Thế Giới.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={() => setShowCreateModal(true)}
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-500 hover:to-blue-500 text-xs font-bold text-white shadow-lg shadow-sky-600/20 transition-all cursor-pointer"
          >
            <Plus size={15} />
            Tạo Không Gian Hub Mới
          </button>

          <button
            onClick={loadNotebooks}
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-slate-900 border border-slate-800 hover:border-slate-700 text-xs font-bold text-slate-300 hover:text-white transition-all cursor-pointer"
          >
            <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
            Làm mới
          </button>
        </div>
      </div>

      {/* UPLOAD ZONE */}
      <div className="bg-gradient-to-br from-purple-900/15 via-indigo-950/10 to-slate-900/40 rounded-3xl border border-purple-500/20 p-8 md:p-10 relative overflow-hidden backdrop-blur-xl shadow-2xl">
        <div className="absolute top-0 right-0 w-64 h-64 bg-purple-500/10 rounded-full blur-3xl pointer-events-none" />
        <div className="text-center space-y-4 relative z-10 max-w-xl mx-auto">
          <div className="w-16 h-16 bg-purple-950/60 border border-purple-500/30 text-purple-400 rounded-3xl flex items-center justify-center text-3xl mx-auto shadow-inner">
            <Brain />
          </div>
          <div className="space-y-2">
            <h2 className="text-xl font-bold text-slate-100">Kéo Thả Tài Liệu Đa Định Dạng Lên Đây</h2>
            <p className="text-xs text-slate-400 leading-relaxed">
              Hỗ trợ đồng thời <strong>PDF, Word (.docx), Excel (.xlsx)</strong> hoặc Web URL. AI tự động bóc tách phân tầng, cảm biến xung đột văn bản, nạp vai trò đàm thoại Socratic và xuất bản đề thi chuẩn hóa IRT 3-PL.
            </p>
          </div>

          <input 
            type="file" 
            ref={fileInputRef} 
            onChange={handleFileChange} 
            accept=".pdf,.docx,.xlsx,.xls,.txt" 
            className="hidden" 
          />

          {uploading ? (
            <div className="space-y-3 pt-2">
              <div className="w-full bg-slate-950 border border-slate-800 rounded-full h-3 overflow-hidden shadow-inner">
                <div 
                  className="bg-gradient-to-r from-purple-500 to-indigo-500 h-3 rounded-full transition-all duration-500 ease-out" 
                  style={{ width: `${progress}%` }} 
                />
              </div>
              <p className="text-xs text-purple-300 font-bold flex items-center justify-center gap-1.5 animate-pulse">
                <Loader2 size={13} className="animate-spin" />
                {statusMessage}
              </p>
            </div>
          ) : (
            <div className="pt-2 flex items-center justify-center gap-3">
              <button 
                onClick={handlePickFile} 
                className="inline-flex items-center gap-2 bg-gradient-to-r from-purple-600 to-indigo-600 hover:from-purple-500 hover:to-indigo-500 text-white px-7 py-3.5 rounded-2xl font-bold transition-all shadow-xl shadow-purple-600/25 active:scale-95 text-xs tracking-wide uppercase cursor-pointer"
              >
                <Sparkles size={16} /> Tải Tệp Lên Ngay &rarr;
              </button>
            </div>
          )}
        </div>
      </div>

      {/* NOTEBOOKS GRID */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-sm font-black text-slate-300 uppercase tracking-wider flex items-center gap-2">
            <BookOpen size={16} className="text-purple-400" /> Không Gian Tri Thức & Hồ Sơ Nhận Thức ({notebooks.length})
          </h3>
        </div>

        {loading ? (
          <div className="p-16 text-center">
            <Loader2 size={32} className="text-purple-500 animate-spin mx-auto mb-3" />
            <p className="text-xs text-slate-400 font-medium">Đang tải danh sách hồ sơ nhận thức...</p>
          </div>
        ) : notebooks.length === 0 ? (
          <div className="p-16 text-center bg-slate-900/40 rounded-3xl border border-slate-800 space-y-3">
            <div className="w-12 h-12 rounded-full bg-slate-800 text-slate-400 flex items-center justify-center mx-auto text-xl">
              <FileText />
            </div>
            <p className="text-sm font-bold text-slate-200">Chưa có Không Gian Tri Thức nào</p>
            <p className="text-xs text-slate-400 max-w-sm mx-auto">
              Hãy bấm "Tạo Không Gian Hub Mới" hoặc tải tệp tài liệu đầu tiên để bắt đầu hành trình khảo thí nhận thức đỉnh cao.
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {notebooks.map((nb) => (
              <div 
                key={nb.id} 
                onClick={() => openStudio(nb.id)}
                className="bg-slate-900/70 rounded-3xl border border-slate-800 p-6 flex flex-col justify-between hover:border-purple-500/40 hover:bg-slate-900 transition-all backdrop-blur-xl relative group shadow-lg cursor-pointer"
              >
                <div className="absolute top-0 right-0 w-28 h-28 bg-purple-500/5 rounded-full blur-2xl group-hover:bg-purple-500/10 transition-all" />
                
                <div className="space-y-4 relative z-10">
                  <div className="flex justify-between items-start gap-4">
                    <h4 className="font-bold text-slate-100 leading-snug line-clamp-2 text-base group-hover:text-purple-300 transition-colors">
                      {nb.title}
                    </h4>
                    <span className="px-2.5 py-1 rounded-full text-[10px] font-black uppercase tracking-wider shrink-0 bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                      Sẵn sàng
                    </span>
                  </div>

                  <p className="text-xs text-slate-400 line-clamp-2 leading-relaxed">
                    {nb.description}
                  </p>

                  <div className="flex flex-wrap items-center gap-2 pt-1">
                    <span className="px-2.5 py-1 rounded-lg bg-sky-950/60 border border-sky-800 text-sky-400 font-bold text-xs flex items-center gap-1.5">
                      <BookOpen size={12} /> {nb.sourceCount || (nb.originalFileName ? 1 : 0)} Nguồn
                    </span>
                    <span className="px-2.5 py-1 rounded-lg bg-slate-950 border border-slate-800 text-purple-300 font-bold text-xs flex items-center gap-1.5">
                      <Layers size={12} /> {nb.chapterCount} Chủ đề
                    </span>
                    <span className="px-2.5 py-1 rounded-lg bg-slate-950 border border-slate-800 text-emerald-300 font-bold text-xs flex items-center gap-1.5">
                      <ListTodo size={12} /> {nb.totalQuestions} Câu hỏi
                    </span>
                  </div>
                </div>

                <div className="mt-6 pt-4 border-t border-slate-800/80 flex items-center justify-between text-xs text-slate-500 relative z-10">
                  <span className="truncate max-w-[150px] font-mono">
                    Lĩnh vực: {nb.domainCode}
                  </span>
                  <div className="flex items-center gap-2">
                    <button 
                      onClick={(e) => handleDelete(e, nb.id)} 
                      className="p-1.5 hover:text-red-400 hover:bg-slate-800 rounded-lg transition-colors cursor-pointer"
                      title="Xóa Không Gian Tri Thức"
                    >
                      <Trash2 size={14} />
                    </button>
                    <span className="inline-flex items-center gap-1 text-purple-400 font-bold group-hover:translate-x-0.5 transition-transform">
                      Khám phá Studio &rarr;
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* MODAL TẠO KHÔNG GIAN HUB MỚI */}
      {showCreateModal && (
        <div className="fixed inset-0 z-50 bg-black/80 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-3xl w-full max-w-lg p-6 space-y-5 shadow-2xl">
            <div className="flex items-center justify-between pb-3 border-b border-slate-800">
              <h3 className="text-base font-bold text-white flex items-center gap-2">
                <Brain className="w-5 h-5 text-sky-400" />
                Khởi Tạo Không Gian Tri Thức Hub
              </h3>
              <button onClick={() => setShowCreateModal(false)} className="text-slate-400 hover:text-white">
                ✕
              </button>
            </div>

            <form onSubmit={handleCreateHub} className="space-y-4">
              <div>
                <label className="text-xs font-semibold text-slate-300 block mb-1">
                  Tiêu Đề Không Gian Tri Thức <span className="text-red-400">*</span>
                </label>
                <input
                  type="text"
                  required
                  value={hubTitle}
                  onChange={(e) => setHubTitle(e.target.value)}
                  placeholder="Ví dụ: Pháp Chế & Thẩm Định Tín Dụng Agribank 2026..."
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-4 py-2.5 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-sky-500"
                />
              </div>

              <div>
                <label className="text-xs font-semibold text-slate-300 block mb-1">
                  Mô Tả Mục Tiêu Khảo Thí:
                </label>
                <textarea
                  rows={2}
                  value={hubDescription}
                  onChange={(e) => setHubDescription(e.target.value)}
                  placeholder="Mô tả tóm tắt mục đích tập hợp tài liệu và mục tiêu đánh giá năng lực..."
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-4 py-2 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-sky-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-xs font-semibold text-slate-300 block mb-1">
                    Lĩnh Vực Chuyên Ngành:
                  </label>
                  <select
                    value={hubDomain}
                    onChange={(e) => setHubDomain(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none focus:border-sky-500"
                  >
                    <option value="GENERAL">Tổng hợp / Đại cương</option>
                    <option value="BANKING">Tài chính - Ngân hàng</option>
                    <option value="EDUCATION">Giáo dục & Học thuật</option>
                    <option value="HEALTHCARE">Y tế - Sức khỏe</option>
                    <option value="HSE">An toàn - Môi trường LĐ</option>
                    <option value="IT_SECURITY">An toàn TT & CNTT</option>
                    <option value="GOV_DRIVING">Sát hạch Lái xe Quốc gia</option>
                  </select>
                </div>

                <div>
                  <label className="text-xs font-semibold text-slate-300 block mb-1">
                    Vai Trò Đối Thoại Khởi Tạo:
                  </label>
                  <select
                    value={hubPersonaRole}
                    onChange={(e) => setHubPersonaRole(Number(e.target.value))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none focus:border-sky-500"
                  >
                    <option value={1}>🎓 Gia sư Socratic</option>
                    <option value={2}>⚖️ Thanh tra Pháp chế</option>
                    <option value={3}>🏛️ Cố vấn Điều hành</option>
                    <option value={4}>🏆 Trưởng Ban Giám Khảo</option>
                    <option value={5}>🛠️ Chuyên gia Kỹ thuật</option>
                  </select>
                </div>
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setShowCreateModal(false)}
                  className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={creatingHub || !hubTitle.trim()}
                  className="px-5 py-2 rounded-xl bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-500 hover:to-blue-500 text-white text-xs font-bold shadow-lg shadow-sky-600/20 disabled:opacity-50"
                >
                  {creatingHub ? 'Đang tạo...' : 'Tạo Không Gian & Vào Studio'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* DUAL-PANE COGNITIVE STUDIO MODAL */}
      {selectedNotebookId && (
        <NotebookDualPaneStudio
          notebookId={selectedNotebookId}
          isOpen={studioOpen}
          onClose={() => {
            setStudioOpen(false);
            setSelectedNotebookId(null);
          }}
          onRefreshList={loadNotebooks}
        />
      )}
    </div>
  );
}
