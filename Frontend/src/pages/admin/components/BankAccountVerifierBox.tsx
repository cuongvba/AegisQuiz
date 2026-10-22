import React, { useState } from 'react';
import { Landmark, Search, ShieldCheck, AlertCircle, CheckCircle2, RefreshCw } from 'lucide-react';
import api from '@/services/api';

export const VIETNAM_BANKS = [
  { bin: '970405', code: 'VBA', name: 'Agribank (Nông Nghiệp & PTNT Việt Nam)' },
  { bin: '970422', code: 'MB', name: 'MB Bank (Ngân Hàng Quân Đội)' },
  { bin: '970436', code: 'VCB', name: 'Vietcombank (Ngoại Thương Việt Nam)' },
  { bin: '970407', code: 'TCB', name: 'Techcombank (Kỹ Thương Việt Nam)' },
  { bin: '970418', code: 'BIDV', name: 'BIDV (Đầu Tư và Phát Triển)' },
  { bin: '970415', code: 'CTG', name: 'VietinBank (Công Thương Việt Nam)' },
  { bin: '970416', code: 'ACB', name: 'ACB (Á Châu)' },
  { bin: '970423', code: 'TPB', name: 'TPBank (Tiên Phong)' },
  { bin: '970432', code: 'VPB', name: 'VPBank (Việt Nam Thịnh Vượng)' },
  { bin: '970403', code: 'STB', name: 'Sacombank (Sài Gòn Thương Tín)' },
];

interface BankAccountVerifierBoxProps {
  bankBin: string;
  accountNumber: string;
  accountName: string;
  readOnly?: boolean;
  onBankChange: (bin: string, bankName: string) => void;
  onAccountNumberChange: (acc: string) => void;
  onAccountNameChange: (name: string) => void;
}

export const BankAccountVerifierBox: React.FC<BankAccountVerifierBoxProps> = ({
  bankBin,
  accountNumber,
  accountName,
  readOnly = false,
  onBankChange,
  onAccountNumberChange,
  onAccountNameChange,
}) => {
  const [lookingUp, setLookingUp] = useState(false);
  const [lookupResult, setLookupResult] = useState<{
    isValid: boolean;
    accountName?: string;
    message: string;
    traceId?: string;
  } | null>(null);

  const handleLookup = async () => {
    if (!accountNumber || accountNumber.trim().length < 6) {
      setLookupResult({
        isValid: false,
        message: 'Vui lòng nhập tối thiểu 6 ký tự số tài khoản trước khi tra cứu.',
      });
      return;
    }

    setLookingUp(true);
    setLookupResult(null);
    try {
      const res = await api.post('/api/payment/lookup-account', {
        bankBin,
        accountNumber: accountNumber.trim(),
      });
      if (res.data) {
        setLookupResult({
          isValid: res.data.isValid,
          accountName: res.data.accountHolderName,
          message: res.data.message,
          traceId: res.data.traceId,
        });
        if (res.data.isValid && res.data.accountHolderName) {
          onAccountNameChange(res.data.accountHolderName);
        }
      }
    } catch (err: any) {
      setLookupResult({
        isValid: false,
        message: err.response?.data?.message || 'Không thể kết nối mạng lưới Napas. Vui lòng thử lại.',
      });
    } finally {
      setLookingUp(false);
    }
  };

  return (
    <div className="bg-slate-900/60 p-5 md:p-6 rounded-3xl border border-slate-800 space-y-4 backdrop-blur-xl">
      <div className="flex items-center justify-between pb-3 border-b border-slate-800">
        <div className="flex items-center gap-2">
          <Landmark className="w-5 h-5 text-emerald-400" />
          <h3 className="font-bold text-slate-100 text-sm md:text-base">1. Tài Khoản Ngân Hàng Thụ Hưởng (VietQR Napas 24/7)</h3>
        </div>
        <span className="text-[10px] text-emerald-400 px-2.5 py-0.5 rounded-full bg-emerald-500/10 border border-emerald-500/20 font-bold uppercase tracking-wider flex items-center gap-1">
          <ShieldCheck className="w-3 h-3" /> Core Napas Verified
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
        <div>
          <label className="block text-slate-300 font-semibold mb-1">Ngân hàng thụ hưởng</label>
          <select
            value={bankBin}
            disabled={readOnly}
            onChange={(e) => {
              const b = VIETNAM_BANKS.find((x) => x.bin === e.target.value);
              onBankChange(e.target.value, b ? b.name : '');
              setLookupResult(null);
            }}
            className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2.5 text-slate-200 focus:border-cyan-500 focus:outline-none disabled:opacity-60"
          >
            {VIETNAM_BANKS.map((b) => (
              <option key={b.bin} value={b.bin}>
                {b.name} ({b.code})
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-slate-300 font-semibold mb-1">Số tài khoản nhận tiền</label>
          <div className="flex gap-2">
            <input
              type="text"
              required
              disabled={readOnly}
              value={accountNumber}
              onChange={(e) => {
                onAccountNumberChange(e.target.value);
                setLookupResult(null);
              }}
              placeholder="VD: 8888912600688"
              className="flex-1 bg-slate-950 border border-slate-700 rounded-xl px-3 py-2.5 text-cyan-400 font-mono font-bold focus:border-cyan-500 focus:outline-none disabled:opacity-60"
            />
            <button
              type="button"
              disabled={lookingUp || readOnly}
              onClick={handleLookup}
              className="px-3.5 py-2 rounded-xl bg-cyan-600/20 hover:bg-cyan-600/30 border border-cyan-500/40 text-cyan-300 font-medium flex items-center gap-1.5 transition disabled:opacity-50 text-xs shrink-0"
              title="Tra cứu xác thực tên chủ tài khoản qua Napas"
            >
              {lookingUp ? <RefreshCw className="w-3.5 h-3.5 animate-spin text-cyan-400" /> : <Search className="w-3.5 h-3.5" />}
              <span>Tra cứu</span>
            </button>
          </div>
        </div>

        <div className="md:col-span-2">
          <label className="block text-slate-300 font-semibold mb-1">Tên chủ tài khoản thụ hưởng (In hoa không dấu)</label>
          <input
            type="text"
            required
            disabled={readOnly}
            value={accountName}
            onChange={(e) => onAccountNameChange(e.target.value.toUpperCase())}
            placeholder="VD: NGUYEN VIET CUONG"
            className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2.5 text-slate-100 font-bold tracking-wide uppercase focus:border-cyan-500 focus:outline-none disabled:opacity-60"
          />
        </div>
      </div>

      {lookupResult && (
        <div
          className={`p-3 rounded-2xl flex items-start gap-2.5 text-xs transition-all ${
            lookupResult.isValid
              ? 'bg-emerald-500/10 border border-emerald-500/30 text-emerald-300'
              : 'bg-rose-500/10 border border-rose-500/30 text-rose-300'
          }`}
        >
          {lookupResult.isValid ? (
            <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0 mt-0.5" />
          ) : (
            <AlertCircle className="w-4 h-4 text-rose-400 shrink-0 mt-0.5" />
          )}
          <div className="space-y-0.5">
            <p className="font-semibold">{lookupResult.message}</p>
            {lookupResult.traceId && (
              <p className="font-mono text-[10px] text-slate-400">Mã giao dịch kiểm thử: {lookupResult.traceId}</p>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
