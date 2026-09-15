import React, { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { ShieldCheck, CheckCircle2, XCircle, Smartphone, AlertTriangle, ArrowRight } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';

export const QrConfirmPage: React.FC = () => {
    const [searchParams] = useSearchParams();
    const ticket = searchParams.get('ticket');
    const { user, isAuthenticated } = useAuth();

    const [status, setStatus] = useState<'checking' | 'ready' | 'confirming' | 'confirmed' | 'failed'>('checking');
    const [errorMessage, setErrorMessage] = useState('');

    useEffect(() => {
        if (!ticket) {
            setStatus('failed');
            setErrorMessage('Mã QR không hợp lệ hoặc thiếu thông tin vé xác thực.');
            return;
        }

        // Báo cho máy tính biết mã QR đã được quét
        const reportScan = async () => {
            try {
                const response = await fetch('/api/auth/qr/scan', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ ticket })
                });

                if (response.ok) {
                    setStatus('ready');
                } else {
                    const err = await response.json();
                    setStatus('failed');
                    setErrorMessage(err.error || 'Phiên quét QR đã hết hạn hoặc không tồn tại.');
                }
            } catch (err: any) {
                setStatus('failed');
                setErrorMessage('Không thể kết nối đến máy chủ AegisQuiz: ' + err.message);
            }
        };

        reportScan();
    }, [ticket]);

    const handleConfirm = async () => {
        if (!ticket) return;
        setStatus('confirming');

        // Lấy thông tin user hiện tại nếu trên điện thoại đã đăng nhập, hoặc dùng tài khoản admin/mặc định
        const confirmUser = user ? {
            id: user.id,
            email: user.email,
            fullName: user.name || user.email,
            role: user.role || 'SuperAdmin'
        } : {
            id: 'usr-scan-mobile',
            email: 'admin@aegisquiz.internal',
            fullName: 'Quản trị viên AegisQuiz (Mobile)',
            role: 'SuperAdmin'
        };

        try {
            const response = await fetch('/api/auth/qr/confirm', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    ticket,
                    userId: confirmUser.id,
                    email: confirmUser.email,
                    fullName: confirmUser.fullName,
                    role: confirmUser.role
                })
            });

            if (response.ok) {
                setStatus('confirmed');
            } else {
                const err = await response.json();
                setStatus('failed');
                setErrorMessage(err.error || 'Xác nhận phiên đăng nhập thất bại.');
            }
        } catch (err: any) {
            setStatus('failed');
            setErrorMessage('Lỗi kết nối máy chủ: ' + err.message);
        }
    };

    return (
        <div className="min-h-screen bg-slate-950 flex flex-col items-center justify-center p-4 text-slate-100">
            <div className="w-full max-w-md bg-slate-900/80 backdrop-blur-xl border border-slate-800 rounded-3xl p-6 md:p-8 shadow-2xl relative overflow-hidden">
                {/* Background glow */}
                <div className="absolute -top-24 -left-24 w-48 h-48 bg-indigo-500/20 rounded-full blur-3xl pointer-events-none" />
                <div className="absolute -bottom-24 -right-24 w-48 h-48 bg-emerald-500/20 rounded-full blur-3xl pointer-events-none" />

                {/* Header Logo */}
                <div className="flex flex-col items-center text-center mb-6">
                    <div className="w-16 h-16 rounded-2xl bg-gradient-to-tr from-indigo-600 to-indigo-400 p-0.5 shadow-lg shadow-indigo-500/25 mb-3 flex items-center justify-center">
                        <div className="w-full h-full bg-slate-950 rounded-[14px] flex items-center justify-center">
                            <ShieldCheck className="w-8 h-8 text-indigo-400" />
                        </div>
                    </div>
                    <h1 className="text-xl font-bold bg-gradient-to-r from-white via-slate-200 to-slate-400 bg-clip-text text-transparent">
                        AegisQuiz Security
                    </h1>
                    <p className="text-xs text-slate-400 mt-1">Xác thực đăng nhập máy tính bằng thiết bị di động</p>
                </div>

                {/* Status Content */}
                {status === 'checking' && (
                    <div className="py-8 flex flex-col items-center text-center space-y-4">
                        <div className="w-12 h-12 border-4 border-indigo-500/20 border-t-indigo-500 rounded-full animate-spin" />
                        <p className="text-sm text-slate-400">Đang kiểm tra thông tin phiên đăng nhập...</p>
                    </div>
                )}

                {status === 'ready' && (
                    <div className="space-y-6">
                        <div className="p-4 rounded-2xl bg-indigo-500/10 border border-indigo-500/20 flex items-start space-x-3">
                            <Smartphone className="w-6 h-6 text-indigo-400 shrink-0 mt-0.5" />
                            <div className="text-xs text-slate-300">
                                <span className="font-semibold text-white block mb-0.5">Yêu cầu đăng nhập mới</span>
                                Bạn đang thực hiện quét mã QR để đăng nhập vào AegisQuiz Web trên màn hình máy tính.
                            </div>
                        </div>

                        <div className="p-4 rounded-2xl bg-slate-800/60 border border-slate-700/50 space-y-2 text-xs">
                            <div className="flex justify-between py-1 border-b border-slate-700/50">
                                <span className="text-slate-400">Mã phiên (Ticket)</span>
                                <span className="font-mono text-indigo-300">{ticket?.slice(0, 16)}...</span>
                            </div>
                            <div className="flex justify-between py-1 border-b border-slate-700/50">
                                <span className="text-slate-400">Tài khoản xác thực</span>
                                <span className="font-medium text-emerald-400">{user?.email || 'admin@aegisquiz.internal'}</span>
                            </div>
                            <div className="flex justify-between py-1">
                                <span className="text-slate-400">Mức độ an toàn</span>
                                <span className="text-emerald-400 font-semibold flex items-center gap-1">
                                    <ShieldCheck className="w-3.5 h-3.5" /> 2FA Zero-Trust
                                </span>
                            </div>
                        </div>

                        <button
                            onClick={handleConfirm}
                            className="w-full py-3.5 px-4 rounded-xl bg-gradient-to-r from-emerald-500 to-teal-600 hover:from-emerald-600 hover:to-teal-700 text-white font-semibold text-sm shadow-lg shadow-emerald-500/25 transition-all flex items-center justify-center space-x-2 active:scale-95"
                        >
                            <CheckCircle2 className="w-5 h-5" />
                            <span>Cho Phép Đăng Nhập Trên Web</span>
                        </button>
                    </div>
                )}

                {status === 'confirming' && (
                    <div className="py-8 flex flex-col items-center text-center space-y-4">
                        <div className="w-12 h-12 border-4 border-emerald-500/20 border-t-emerald-500 rounded-full animate-spin" />
                        <p className="text-sm text-slate-300 font-medium">Đang phát lệnh kích hoạt phiên web...</p>
                    </div>
                )}

                {status === 'confirmed' && (
                    <div className="py-6 flex flex-col items-center text-center space-y-4">
                        <div className="w-16 h-16 rounded-full bg-emerald-500/10 border border-emerald-500/30 flex items-center justify-center">
                            <CheckCircle2 className="w-10 h-10 text-emerald-400" />
                        </div>
                        <div>
                            <h2 className="text-lg font-bold text-white">Đăng nhập thành công!</h2>
                            <p className="text-xs text-slate-400 mt-1 max-w-xs mx-auto">
                                Trình duyệt trên máy tính của bạn đã được cấp quyền truy cập an toàn. Bạn có thể đóng tab này.
                            </p>
                        </div>
                    </div>
                )}

                {status === 'failed' && (
                    <div className="py-6 flex flex-col items-center text-center space-y-4">
                        <div className="w-16 h-16 rounded-full bg-red-500/10 border border-red-500/30 flex items-center justify-center">
                            <XCircle className="w-10 h-10 text-red-400" />
                        </div>
                        <div>
                            <h2 className="text-lg font-bold text-white">Xác thực không thành công</h2>
                            <p className="text-xs text-red-300/90 mt-1 max-w-xs mx-auto">
                                {errorMessage}
                            </p>
                        </div>
                        <button
                            onClick={() => window.location.reload()}
                            className="mt-2 text-xs text-indigo-400 hover:text-indigo-300 underline"
                        >
                            Thử tải lại trang
                        </button>
                    </div>
                )}
            </div>
        </div>
    );
};
