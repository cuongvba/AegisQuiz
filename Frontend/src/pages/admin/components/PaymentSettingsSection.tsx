import React, { useState, useEffect } from 'react';
import { Key, ShieldCheck, RefreshCw, Check, Copy, Save, AlertCircle, Eye, EyeOff, Zap, ShieldAlert } from 'lucide-react';
import api from '@/services/api';
import { BankAccountVerifierBox } from './BankAccountVerifierBox';
import { MfaStepUpModal } from './MfaStepUpModal';

export const PaymentSettingsSection: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [statusMsg, setStatusMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const [showKeys, setShowKeys] = useState(false);
  const [copiedWebhook, setCopiedWebhook] = useState(false);
  const [isMfaModalOpen, setIsMfaModalOpen] = useState(false);

  // Kiểm tra quyền SysAdmin từ LocalStorage Auth State
  const [isSysAdmin, setIsSysAdmin] = useState(true);

  const [form, setForm] = useState({
    vietQrBankBin: '970405',
    vietQrBankName: 'Agribank (Nông Nghiệp & PTNT Việt Nam)',
    vietQrAccountNumber: '8888912600688',
    vietQrAccountName: 'NGUYEN VIET CUONG',
    payOsClientId: '',
    payOsApiKey: '',
    payOsChecksumKey: '',
    payOsWebhookUrl: `${window.location.origin}/api/webhook/payos`,
    stripePublishableKey: '',
    stripeSecretKey: '',
    web3TreasuryAddress: '0x16b607ED8d8a7c2937DF0EAc2Cd0B47a759600eE',
    enableSandboxTesting: true,
  });

  useEffect(() => {
    try {
      const userRaw = localStorage.getItem('user');
      if (userRaw) {
        const u = JSON.parse(userRaw);
        const role = (u.role || u.Role || '').toLowerCase();
        setIsSysAdmin(role.includes('systemadmin') || role.includes('sysadmin'));
      }
    } catch {
      setIsSysAdmin(true);
    }
    fetchConfig();
  }, []);

  const fetchConfig = async () => {
    setLoading(true);
    try {
      const res = await api.get('/api/payment/config');
      if (res.data) {
        setForm((prev) => ({
          ...prev,
          ...res.data,
          payOsWebhookUrl: res.data.payOsWebhookUrl || `${window.location.origin}/api/webhook/payos`,
        }));
      }
    } catch (err) {
      console.error('Fetch payment config failed:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleMfaConfirmed = async (authData: { sudoPassword?: string; mfaVerificationCode?: string }) => {
    setSaving(true);
    setStatusMsg(null);
    try {
      const payload = {
        ...form,
        ...authData,
      };
      await api.post('/api/payment/config', payload);
      setStatusMsg({ type: 'success', text: 'Đã xác thực MFA & lưu cấu hình tài khoản thụ hưởng thành công! 0ms Hot-Reload kích hoạt.' });
      setIsMfaModalOpen(false);
    } catch (err: any) {
      setStatusMsg({ type: 'error', text: err.response?.data?.message || 'Lỗi lưu cấu hình hoặc xác thực thất bại.' });
    } finally {
      setSaving(false);
    }
  };

  const handleTestConnection = async () => {
    setTesting(true);
    setStatusMsg(null);
    try {
      const res = await api.post('/api/payment/test-connection?provider=vietqr');
      if (res.data?.connected) {
        setStatusMsg({ type: 'success', text: 'Kết nối mạng lưới Napas & Cổng thanh toán hoạt động hoàn hảo!' });
      }
    } catch {
      setStatusMsg({ type: 'error', text: 'Kiểm tra kết nối thất bại.' });
    } finally {
      setTesting(false);
    }
  };

  if (loading) {
    return (
      <div className="p-12 text-center text-slate-400">
        <RefreshCw className="w-8 h-8 animate-spin mx-auto text-cyan-400 mb-2" />
        <p className="text-sm">Đang tải cấu hình cổng thanh toán...</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {!isSysAdmin && (
        <div className="p-4 rounded-2xl bg-amber-500/10 border border-amber-500/30 text-amber-300 text-xs flex items-center gap-3">
          <ShieldAlert className="w-5 h-5 shrink-0 text-amber-400" />
          <div>
            <p className="font-bold">Chế độ Chỉ Xem (SysAdmin Sovereignty Protocol)</p>
            <p className="text-amber-400/80">Bạn đang xem với tư cách Quản trị viên Tenant. Thẩm quyền cấu hình tài khoản thụ hưởng tài chính thuộc về Quản trị viên Tối cao (SysAdmin).</p>
          </div>
        </div>
      )}

      {statusMsg && (
        <div
          className={`p-4 rounded-2xl flex items-center gap-3 text-xs font-semibold ${
            statusMsg.type === 'success'
              ? 'bg-emerald-500/10 border border-emerald-500/20 text-emerald-300'
              : 'bg-rose-500/10 border border-rose-500/20 text-rose-300'
          }`}
        >
          {statusMsg.type === 'success' ? <Check className="w-4 h-4" /> : <AlertCircle className="w-4 h-4" />}
          <span>{statusMsg.text}</span>
        </div>
      )}

      {/* 1. VietQR Napas Direct với Real-Time Verifier Sub-component */}
      <BankAccountVerifierBox
        bankBin={form.vietQrBankBin}
        accountNumber={form.vietQrAccountNumber}
        accountName={form.vietQrAccountName}
        readOnly={!isSysAdmin}
        onBankChange={(bin, bankName) => setForm((p) => ({ ...p, vietQrBankBin: bin, vietQrBankName: bankName }))}
        onAccountNumberChange={(acc) => setForm((p) => ({ ...p, vietQrAccountNumber: acc }))}
        onAccountNameChange={(name) => setForm((p) => ({ ...p, vietQrAccountName: name }))}
      />

      {/* 2. PayOS API Credentials */}
      <div className="bg-slate-900/60 p-6 rounded-3xl border border-slate-800 space-y-4 backdrop-blur-xl">
        <div className="flex items-center justify-between pb-3 border-b border-slate-800">
          <div className="flex items-center gap-2">
            <Key className="w-5 h-5 text-cyan-400" />
            <h3 className="font-bold text-slate-100 text-sm md:text-base">2. Tích Hợp Cổng Tự Động PayOS (Napas Webhook)</h3>
          </div>
          <button
            type="button"
            onClick={() => setShowKeys(!showKeys)}
            className="flex items-center gap-1 text-slate-400 hover:text-slate-200 text-xs"
          >
            {showKeys ? <EyeOff className="w-3.5 h-3.5" /> : <Eye className="w-3.5 h-3.5" />}
            <span>{showKeys ? 'Ẩn khóa bảo mật' : 'Hiện khóa bảo mật'}</span>
          </button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-3 text-xs">
          <div>
            <label className="block text-slate-300 font-medium mb-1">Client ID</label>
            <input
              type={showKeys ? 'text' : 'password'}
              disabled={!isSysAdmin}
              placeholder="••••••••••••••••••••••••••••••••"
              value={form.payOsClientId}
              onChange={(e) => setForm({ ...form, payOsClientId: e.target.value })}
              className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 font-mono text-slate-300 focus:border-cyan-500 focus:outline-none disabled:opacity-60"
            />
          </div>
          <div>
            <label className="block text-slate-300 font-medium mb-1">API Key</label>
            <input
              type={showKeys ? 'text' : 'password'}
              disabled={!isSysAdmin}
              placeholder="••••••••••••••••••••••••••••••••"
              value={form.payOsApiKey}
              onChange={(e) => setForm({ ...form, payOsApiKey: e.target.value })}
              className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 font-mono text-slate-300 focus:border-cyan-500 focus:outline-none disabled:opacity-60"
            />
          </div>
          <div>
            <label className="block text-slate-300 font-medium mb-1">Checksum Key (HMAC-SHA256)</label>
            <input
              type={showKeys ? 'text' : 'password'}
              disabled={!isSysAdmin}
              placeholder="••••••••••••••••••••••••••••••••"
              value={form.payOsChecksumKey}
              onChange={(e) => setForm({ ...form, payOsChecksumKey: e.target.value })}
              className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 font-mono text-slate-300 focus:border-cyan-500 focus:outline-none disabled:opacity-60"
            />
          </div>
        </div>

        <div className="p-3 rounded-xl bg-slate-950/80 border border-slate-800 flex items-center justify-between text-xs">
          <div className="space-y-0.5">
            <span className="text-slate-400 font-medium">Webhook URL đăng ký trên PayOS:</span>
            <p className="font-mono text-cyan-400 text-[11px] select-all">{form.payOsWebhookUrl}</p>
          </div>
          <button
            type="button"
            onClick={() => {
              navigator.clipboard.writeText(form.payOsWebhookUrl);
              setCopiedWebhook(true);
              setTimeout(() => setCopiedWebhook(false), 2000);
            }}
            className="p-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300"
          >
            {copiedWebhook ? <Check className="w-4 h-4 text-emerald-400" /> : <Copy className="w-4 h-4" />}
          </button>
        </div>
      </div>

      {/* 3. Global Stripe & Web3 Treasury */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="bg-slate-900/60 p-5 rounded-3xl border border-slate-800 space-y-3 backdrop-blur-xl">
          <h4 className="font-bold text-slate-100 text-sm flex items-center gap-2">
            <ShieldCheck className="w-4 h-4 text-blue-400" /> 3. Stripe & Global Card
          </h4>
          <div className="space-y-2 text-xs">
            <div>
              <label className="block text-slate-400 mb-1">Publishable Key</label>
              <input
                type="text"
                disabled={!isSysAdmin}
                placeholder="pk_live_..."
                value={form.stripePublishableKey}
                onChange={(e) => setForm({ ...form, stripePublishableKey: e.target.value })}
                className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 font-mono text-slate-300 focus:border-blue-500 focus:outline-none disabled:opacity-60"
              />
            </div>
            <div>
              <label className="block text-slate-400 mb-1">Secret Key</label>
              <input
                type={showKeys ? 'text' : 'password'}
                disabled={!isSysAdmin}
                placeholder="sk_live_..."
                value={form.stripeSecretKey}
                onChange={(e) => setForm({ ...form, stripeSecretKey: e.target.value })}
                className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 font-mono text-slate-300 focus:border-blue-500 focus:outline-none disabled:opacity-60"
              />
            </div>
          </div>
        </div>

        <div className="bg-slate-900/60 p-5 rounded-3xl border border-slate-800 space-y-3 backdrop-blur-xl">
          <h4 className="font-bold text-slate-100 text-sm flex items-center gap-2">
            <Zap className="w-4 h-4 text-purple-400" /> 4. Ví Nhận Web3 (Polygon Treasury)
          </h4>
          <div className="space-y-2 text-xs">
            <div>
              <label className="block text-slate-400 mb-1">Địa chỉ Ví nhận MATIC / USDT On-chain</label>
              <input
                type="text"
                disabled={!isSysAdmin}
                value={form.web3TreasuryAddress}
                onChange={(e) => setForm({ ...form, web3TreasuryAddress: e.target.value })}
                className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 font-mono text-purple-400 focus:border-purple-500 focus:outline-none disabled:opacity-60"
              />
            </div>
            <label className="flex items-center gap-2 pt-2 cursor-pointer">
              <input
                type="checkbox"
                disabled={!isSysAdmin}
                checked={form.enableSandboxTesting}
                onChange={(e) => setForm({ ...form, enableSandboxTesting: e.target.checked })}
                className="w-4 h-4 rounded text-cyan-500 bg-slate-950 border-slate-700"
              />
              <span className="text-slate-300">Bật Chế độ Sandbox Testing (Cho phép Giả lập nạp thử nghiệm)</span>
            </label>
          </div>
        </div>
      </div>

      {/* Action Buttons */}
      <div className="flex flex-col sm:flex-row justify-end gap-3 pt-3">
        <button
          type="button"
          onClick={handleTestConnection}
          disabled={testing}
          className="px-5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 border border-slate-700 text-slate-200 text-xs font-semibold flex items-center justify-center gap-2 transition disabled:opacity-50"
        >
          {testing ? <RefreshCw className="w-4 h-4 animate-spin text-cyan-400" /> : <ShieldCheck className="w-4 h-4 text-emerald-400" />}
          Kiểm tra Kết nối (Ping Test)
        </button>
        {isSysAdmin && (
          <button
            type="button"
            onClick={() => setIsMfaModalOpen(true)}
            disabled={saving}
            className="px-6 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 via-cyan-500 to-emerald-500 hover:opacity-90 text-white text-xs font-bold flex items-center justify-center gap-2 shadow-lg shadow-cyan-500/20 transition disabled:opacity-50"
          >
            {saving ? <RefreshCw className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
            Lưu Cấu Hình (0ms Hot-Reload)
          </button>
        )}
      </div>

      {/* Modal Xác Thực Sudo/MFA Cấp Độ 3 */}
      <MfaStepUpModal
        isOpen={isMfaModalOpen}
        onClose={() => setIsMfaModalOpen(false)}
        onConfirm={handleMfaConfirmed}
        isLoading={saving}
      />
    </div>
  );
};
