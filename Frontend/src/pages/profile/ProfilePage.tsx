import React, { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  User, Shield, Key, Sparkles, Building2, Layers, CheckCircle2,
  AlertCircle, Save, Lock, Smartphone, Mail, Calendar, Eye, EyeOff,
  Clock, ArrowRight, Zap, RefreshCw, Check, ArrowLeft, Home, LayoutDashboard,
  X, ChevronRight
} from 'lucide-react';
import { useAuth } from '@/hooks/useAuth';
import api from '@/services/api';
import { AppRole, type UserProfile } from '@/types/auth';
import { TwoFactorProfileSection } from './TwoFactorProfileSection';

export function ProfilePage() {
  const { t, i18n } = useTranslation('profile');
  const navigate = useNavigate();
  const { user } = useAuth();
  const [currentUser, setCurrentUser] = useState<any>(user);

  const [activeTab, setActiveTab] = useState<'info' | 'subscription' | 'security'>('info');

  // Form State - Info
  const [fullName, setFullName] = useState(user?.name || '');
  const [phoneNumber, setPhoneNumber] = useState(user?.phoneNumber || '');
  const [avatarUrl, setAvatarUrl] = useState(user?.avatar || '');
  const [isSavingInfo, setIsSavingInfo] = useState(false);
  const [infoMessage, setInfoMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  // Form State - Password
  const [oldPassword, setOldPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showOldPassword, setShowOldPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [isChangingPassword, setIsChangingPassword] = useState(false);
  const [passwordMessage, setPasswordMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const fetchMe = async () => {
    try {
      const res = await api.get('/api/auth/me');
      if (res.data?.user) {
        const freshUser = res.data.user;
        setCurrentUser(freshUser);
        setFullName(freshUser.name || '');
        setPhoneNumber(freshUser.phoneNumber || '');
        setAvatarUrl(freshUser.avatar || '');
        localStorage.setItem('user', JSON.stringify({ ...user, ...freshUser }));
      }
    } catch (err) {
      console.warn('Could not fetch fresh user profile', err);
    }
  };

  // Sync state when user profile is loaded
  useEffect(() => {
    if (user) {
      setCurrentUser(user);
      setFullName(user.name || '');
      setPhoneNumber(user.phoneNumber || '');
      setAvatarUrl(user.avatar || '');
    }
  }, [user]);

  // Fetch full details from /api/auth/me on mount
  useEffect(() => {
    fetchMe();
  }, []);

  // NIST Password strength calculation
  const getPasswordStrength = (pass: string) => {
    let score = 0;
    if (pass.length >= 8) score += 1;
    if (pass.length >= 12) score += 1;
    if (/[A-Z]/.test(pass)) score += 1;
    if (/[0-9]/.test(pass)) score += 1;
    if (/[^A-Za-z0-9]/.test(pass)) score += 1;
    return score; // max 5
  };

  const passwordScore = getPasswordStrength(newPassword);

  const getStrengthLabel = (score: number) => {
    if (!newPassword) return { label: t('passwordStrength.empty'), color: 'bg-slate-700', text: 'text-slate-500' };
    if (score <= 2) return { label: t('passwordStrength.weak'), color: 'bg-rose-500', text: 'text-rose-400' };
    if (score <= 3) return { label: t('passwordStrength.medium'), color: 'bg-amber-500', text: 'text-amber-400' };
    if (score === 4) return { label: t('passwordStrength.good'), color: 'bg-blue-500', text: 'text-blue-400' };
    return { label: t('passwordStrength.strong'), color: 'bg-emerald-500', text: 'text-emerald-400' };
  };

  const handleUpdateProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSavingInfo(true);
    setInfoMessage(null);
    try {
      const res = await api.put('/api/auth/profile', {
        fullName: fullName.trim(),
        phoneNumber: phoneNumber.trim(),
        avatarUrl: avatarUrl.trim()
      });

      if (res.data?.user) {
        const updated = res.data.user;
        const currentRaw = localStorage.getItem('user');
        const currentObj = currentRaw ? JSON.parse(currentRaw) : {};
        localStorage.setItem('user', JSON.stringify({ ...currentObj, ...updated }));
        window.dispatchEvent(new Event('storage'));
      }

      setInfoMessage({ type: 'success', text: t('messages.infoUpdateSuccess') });
    } catch (err: any) {
      const msg = err.response?.data?.message || t('messages.infoUpdateError');
      setInfoMessage({ type: 'error', text: msg });
    } finally {
      setIsSavingInfo(false);
    }
  };

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordMessage(null);

    if (newPassword.length < 6) {
      setPasswordMessage({ type: 'error', text: t('messages.passwordTooShort') });
      return;
    }

    if (newPassword !== confirmPassword) {
      setPasswordMessage({ type: 'error', text: t('messages.passwordMismatch') });
      return;
    }

    setIsChangingPassword(true);
    try {
      const res = await api.post('/api/auth/change-password', {
        oldPassword,
        newPassword
      });

      setPasswordMessage({
        type: 'success',
        text: res.data?.message || t('messages.passwordChangeSuccess')
      });
      setOldPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (err: any) {
      const msg = err.response?.data?.message || t('messages.passwordChangeError');
      setPasswordMessage({ type: 'error', text: msg });
    } finally {
      setIsChangingPassword(false);
    }
  };

  const role = user?.role || AppRole.Learner;
  const isVip = user?.isPremium || user?.subscriptionTier === 'VIP' || user?.subscriptionTier === 'ENTERPRISE';
  const isAdminRole = [AppRole.SystemAdmin, AppRole.TenantAdmin, AppRole.TeamLeader, AppRole.Instructor].includes(role as any);

  const handleGoBack = () => {
    if (window.history.length > 2) {
      navigate(-1);
    } else {
      navigate(isAdminRole ? '/admin' : '/dashboard');
    }
  };

  const getRoleBadge = (r: string) => {
    switch (r) {
      case AppRole.SystemAdmin:
        return { label: t('roles.systemAdmin'), color: 'bg-rose-500/10 text-rose-400 border-rose-500/20' };
      case AppRole.TenantAdmin:
        return { label: t('roles.tenantAdmin'), color: 'bg-purple-500/10 text-purple-400 border-purple-500/20' };
      case AppRole.TeamLeader:
        return { label: t('roles.teamLeader'), color: 'bg-cyan-500/10 text-cyan-400 border-cyan-500/20' };
      case AppRole.Instructor:
        return { label: t('roles.instructor'), color: 'bg-blue-500/10 text-blue-400 border-blue-500/20' };
      default:
        return { label: t('roles.learner'), color: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20' };
    }
  };

  const roleInfo = getRoleBadge(role);
  const strengthInfo = getStrengthLabel(passwordScore);

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 py-6 sm:py-10 px-4 sm:px-6 lg:px-8">
      <div className="max-w-5xl mx-auto space-y-6">

        {/* ── TOP BREADCRUMB & NAVIGATION BAR ─────────────────────────── */}
        <div className="flex items-center justify-between flex-wrap gap-3 pb-1">
          {/* Back & Breadcrumbs */}
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={handleGoBack}
              className="px-3.5 py-2 rounded-xl bg-slate-900/90 hover:bg-slate-800 text-slate-300 hover:text-white border border-slate-800 hover:border-slate-700 transition-all flex items-center gap-2 text-xs font-bold shadow-sm cursor-pointer"
              title={t('breadcrumb.backTitle')}
            >
              <ArrowLeft size={16} />
              <span>{t('breadcrumb.back')}</span>
            </button>

            <nav className="flex items-center gap-1.5 text-xs text-slate-400 font-medium">
              <Link to="/dashboard" className="hover:text-slate-200 transition-colors flex items-center gap-1">
                <Home size={13} />
                <span>{t('breadcrumb.home')}</span>
              </Link>
              <ChevronRight size={13} className="text-slate-600" />
              <span className="text-slate-200 font-semibold">{t('breadcrumb.current')}</span>
            </nav>
          </div>

          {/* Quick shortcuts */}
          <div className="flex items-center gap-2">
            {isAdminRole && (
              <button
                type="button"
                onClick={() => navigate('/admin')}
                className="px-3 py-1.5 rounded-xl bg-purple-500/10 hover:bg-purple-500/20 text-purple-300 border border-purple-500/30 transition-all text-xs font-bold flex items-center gap-1.5 cursor-pointer"
              >
                <LayoutDashboard size={14} />
                <span>{t('breadcrumb.adminPortal')}</span>
              </button>
            )}

            <button
              type="button"
              onClick={() => navigate('/dashboard')}
              className="px-3 py-1.5 rounded-xl bg-slate-900/80 hover:bg-slate-800 text-slate-300 hover:text-white border border-slate-800 transition-all text-xs font-bold flex items-center gap-1.5 cursor-pointer"
            >
              <Home size={14} />
              <span>{t('breadcrumb.home')}</span>
            </button>
          </div>
        </div>

        {/* ── TOP HERO CARD ──────────────────────────────────────────────── */}
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-b from-slate-900/90 to-slate-900/40 border border-slate-800 p-6 sm:p-8 backdrop-blur-xl shadow-2xl">
          <div className="absolute top-0 right-0 w-96 h-96 bg-blue-600/10 rounded-full blur-3xl pointer-events-none -mr-20 -mt-20" />
          <div className="absolute bottom-0 left-1/3 w-64 h-64 bg-purple-600/10 rounded-full blur-3xl pointer-events-none" />

          {/* Close button in top right of hero card */}
          <button
            type="button"
            onClick={handleGoBack}
            className="absolute top-5 right-5 z-20 p-2 rounded-xl bg-slate-850/80 hover:bg-slate-750 text-slate-400 hover:text-white border border-slate-700/60 transition-all cursor-pointer"
            title={t('breadcrumb.closeTitle')}
          >
            <X size={18} />
          </button>

          <div className="relative z-10 flex flex-col sm:flex-row items-center sm:items-start gap-6">
            {/* Avatar with Glow */}
            <div className="relative group">
              <div className="w-24 h-24 sm:w-28 sm:h-28 rounded-2xl bg-gradient-to-tr from-blue-600 via-indigo-600 to-purple-600 p-[2px] shadow-xl shadow-blue-500/10">
                <div className="w-full h-full bg-slate-900 rounded-[14px] flex items-center justify-center overflow-hidden">
                  {avatarUrl ? (
                    <img src={avatarUrl} alt={user?.name} className="w-full h-full object-cover" />
                  ) : (
                    <span className="text-3xl sm:text-4xl font-black text-transparent bg-clip-text bg-gradient-to-tr from-blue-400 to-purple-400 uppercase">
                      {(user?.name || user?.email || 'U').charAt(0)}
                    </span>
                  )}
                </div>
              </div>
              <div className="absolute -bottom-1 -right-1 w-6 h-6 rounded-full bg-emerald-500 border-2 border-slate-900 flex items-center justify-center shadow-lg">
                <div className="w-2 h-2 rounded-full bg-white animate-pulse" />
              </div>
            </div>

            {/* Profile Info Summary */}
            <div className="flex-1 text-center sm:text-left space-y-2">
              <div className="flex flex-wrap items-center justify-center sm:justify-start gap-2.5">
                <h1 className="text-2xl sm:text-3xl font-black text-white tracking-tight">
                  {user?.name || t('hero.defaultAccountName')}
                </h1>
                <span className={`px-3 py-1 rounded-full text-xs font-bold border tracking-wide flex items-center gap-1.5 ${roleInfo.color}`}>
                  <Shield size={13} />
                  {roleInfo.label}
                </span>
                <span className={`px-3 py-1 rounded-full text-xs font-black tracking-wider uppercase flex items-center gap-1.5 ${
                  isVip
                    ? 'bg-amber-500/10 text-amber-400 border border-amber-500/20'
                    : 'bg-slate-800 text-slate-400 border border-slate-700'
                }`}>
                  <Sparkles size={12} className={isVip ? 'text-amber-400 animate-pulse' : ''} />
                  {user?.subscriptionTier || 'FREE'}
                </span>
              </div>

              <p className="text-sm text-slate-400 font-mono flex items-center justify-center sm:justify-start gap-1.5">
                <Mail size={14} className="text-slate-500" />
                {user?.email}
              </p>

              {/* Scoped Hierarchical OU Breadcrumbs */}
              <div className="pt-2 flex flex-wrap items-center justify-center sm:justify-start gap-2 text-xs font-medium text-slate-400">
                <div className="flex items-center gap-1 bg-slate-800/80 px-3 py-1 rounded-lg border border-slate-700/60">
                  <Building2 size={13} className="text-blue-400" />
                  <span className="text-slate-300 font-semibold">{user?.tenantName || t('hero.defaultTenant')}</span>
                </div>

                <div className="flex items-center gap-1 bg-slate-800/80 px-3 py-1 rounded-lg border border-slate-700/60">
                  <Layers size={13} className="text-purple-400" />
                  <span className="text-slate-300">
                    {user?.orgUnitName || user?.ou || t('hero.defaultOrgUnit')}
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Navigation Tabs */}
          <div className="mt-8 pt-6 border-t border-slate-800/80 flex items-center gap-2 overflow-x-auto scrollbar-none">
            <button
              onClick={() => setActiveTab('info')}
              className={`px-5 py-2.5 rounded-xl font-bold text-sm transition-all flex items-center gap-2 cursor-pointer ${
                activeTab === 'info'
                  ? 'bg-blue-600 text-white shadow-lg shadow-blue-500/20'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
              }`}
            >
              <User size={16} />
              {t('tabs.info')}
            </button>

            <button
              onClick={() => setActiveTab('subscription')}
              className={`px-5 py-2.5 rounded-xl font-bold text-sm transition-all flex items-center gap-2 cursor-pointer ${
                activeTab === 'subscription'
                  ? 'bg-blue-600 text-white shadow-lg shadow-blue-500/20'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
              }`}
            >
              <Sparkles size={16} />
              {t('tabs.subscription')}
            </button>

            <button
              onClick={() => setActiveTab('security')}
              className={`px-5 py-2.5 rounded-xl font-bold text-sm transition-all flex items-center gap-2 cursor-pointer ${
                activeTab === 'security'
                  ? 'bg-blue-600 text-white shadow-lg shadow-blue-500/20'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
              }`}
            >
              <Key size={16} />
              {t('tabs.security')}
            </button>
          </div>
        </div>

        {/* ── TAB 1: THÔNG TIN CÁ NHÂN ───────────────────────────────────── */}
        {activeTab === 'info' && (
          <div className="bg-slate-900/60 border border-slate-800 rounded-3xl p-6 sm:p-8 backdrop-blur-xl shadow-xl space-y-6">
            <div>
              <h2 className="text-xl font-black text-white flex items-center gap-2">
                <User size={20} className="text-blue-400" />
                {t('infoTab.title')}
              </h2>
              <p className="text-xs text-slate-400 mt-1">{t('infoTab.subtitle')}</p>
            </div>

            {infoMessage && (
              <div className={`p-4 rounded-xl flex items-center gap-3 text-sm font-medium border ${
                infoMessage.type === 'success'
                  ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400'
                  : 'bg-rose-500/10 border-rose-500/20 text-rose-400'
              }`}>
                {infoMessage.type === 'success' ? <CheckCircle2 size={18} /> : <AlertCircle size={18} />}
                {infoMessage.text}
              </div>
            )}

            <form onSubmit={handleUpdateProfile} className="space-y-6">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-2">
                    {t('infoTab.fullName')}
                  </label>
                  <input
                    type="text"
                    value={fullName}
                    onChange={(e) => setFullName(e.target.value)}
                    required
                    placeholder={t('infoTab.fullNamePlaceholder')}
                    className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-4 py-3 text-white text-sm focus:outline-none transition-all"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-2">
                    {t('infoTab.phoneNumber')}
                  </label>
                  <div className="relative">
                    <Smartphone size={16} className="absolute left-3.5 top-3.5 text-slate-500" />
                    <input
                      type="text"
                      value={phoneNumber}
                      onChange={(e) => setPhoneNumber(e.target.value)}
                      placeholder={t('infoTab.phonePlaceholder')}
                      className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl pl-10 pr-4 py-3 text-white text-sm focus:outline-none transition-all"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-2">
                    {t('infoTab.emailReadOnly')}
                  </label>
                  <input
                    type="email"
                    value={user?.email || ''}
                    disabled
                    className="w-full bg-slate-950/60 border border-slate-850 rounded-xl px-4 py-3 text-slate-500 text-sm font-mono cursor-not-allowed"
                  />
                  <span className="text-[11px] text-slate-500 mt-1 block">{t('infoTab.emailHelper')}</span>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-2">
                    {t('infoTab.avatarUrl')}
                  </label>
                  <input
                    type="url"
                    value={avatarUrl}
                    onChange={(e) => setAvatarUrl(e.target.value)}
                    placeholder="https://example.com/avatar.jpg"
                    className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-4 py-3 text-white text-sm focus:outline-none transition-all font-mono"
                  />
                  {/* Quick Avatar Presets */}
                  <div className="mt-3">
                    <span className="text-[11px] text-slate-400 font-semibold block mb-1.5">{t('infoTab.avatarPresetsLabel')}</span>
                    <div className="flex items-center gap-2 flex-wrap">
                      {[
                        { name: 'Cyber', url: 'https://api.dicebear.com/7.x/bottts/svg?seed=Cyber' },
                        { name: 'Scholar', url: 'https://api.dicebear.com/7.x/adventurer/svg?seed=Scholar' },
                        { name: 'Leader', url: 'https://api.dicebear.com/7.x/micah/svg?seed=Leader' },
                        { name: 'Matrix', url: 'https://api.dicebear.com/7.x/bottts/svg?seed=Matrix' },
                        { name: 'Aegis', url: 'https://api.dicebear.com/7.x/adventurer/svg?seed=Aegis' },
                      ].map((preset) => (
                        <button
                          key={preset.name}
                          type="button"
                          onClick={() => setAvatarUrl(preset.url)}
                          className={`w-9 h-9 rounded-xl border p-0.5 overflow-hidden transition-all cursor-pointer ${
                            avatarUrl === preset.url
                              ? 'border-blue-500 bg-blue-500/20 scale-110 shadow-md shadow-blue-500/30'
                              : 'border-slate-800 hover:border-slate-600 bg-slate-950'
                          }`}
                          title={t('infoTab.avatarChooseTitle', { name: preset.name })}
                        >
                          <img src={preset.url} alt={preset.name} className="w-full h-full object-cover" />
                        </button>
                      ))}
                    </div>
                  </div>
                </div>
              </div>

              {/* Readonly Organization details */}
              <div className="p-5 rounded-2xl bg-slate-950/50 border border-slate-800/80 space-y-3">
                <h3 className="text-xs font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
                  <Building2 size={14} className="text-blue-400" />
                  {t('infoTab.hierarchyTitle')}
                </h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
                  <div>
                    <span className="text-slate-500 block">{t('infoTab.tenantLabel')}</span>
                    <span className="text-slate-200 font-semibold">{user?.tenantName || t('infoTab.defaultTenant')}</span>
                  </div>
                  <div>
                    <span className="text-slate-500 block">{t('infoTab.orgUnitLabel')}</span>
                    <span className="text-slate-200 font-semibold">{user?.orgUnitName || t('infoTab.defaultOrgUnit')}</span>
                  </div>
                </div>
              </div>

              <div className="flex justify-end pt-2">
                <button
                  type="submit"
                  disabled={isSavingInfo}
                  className="px-6 py-3 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-bold text-sm flex items-center gap-2 shadow-lg shadow-blue-500/25 transition-all cursor-pointer disabled:opacity-50"
                >
                  {isSavingInfo ? <RefreshCw size={16} className="animate-spin" /> : <Save size={16} />}
                  {isSavingInfo ? t('infoTab.saving') : t('infoTab.save')}
                </button>
              </div>
            </form>
          </div>
        )}

        {/* ── TAB 2: THUÊ BAO & ĐẶC QUYỀN ─────────────────────────────────── */}
        {activeTab === 'subscription' && (
          <div className="bg-slate-900/60 border border-slate-800 rounded-3xl p-6 sm:p-8 backdrop-blur-xl shadow-xl space-y-6">
            <div>
              <h2 className="text-xl font-black text-white flex items-center gap-2">
                <Sparkles size={20} className="text-amber-400" />
                {t('subscriptionTab.title')}
              </h2>
              <p className="text-xs text-slate-400 mt-1">{t('subscriptionTab.subtitle')}</p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {/* Card Current Tier */}
              <div className="md:col-span-1 rounded-2xl bg-gradient-to-b from-slate-950 to-slate-900 border border-slate-800 p-6 flex flex-col justify-between">
                <div className="space-y-3">
                  <span className="text-xs font-bold text-slate-400 uppercase tracking-wider">{t('subscriptionTab.currentPlan')}</span>
                  <div className="flex items-baseline gap-2">
                    <span className="text-3xl font-black text-transparent bg-clip-text bg-gradient-to-r from-amber-300 via-amber-400 to-amber-500">
                      {user?.subscriptionTier || 'FREE'}
                    </span>
                  </div>
                  <p className="text-xs text-slate-400">
                    {isVip
                      ? t('subscriptionTab.vipDescription')
                      : t('subscriptionTab.freeDescription')}
                  </p>
                </div>

                <div className="pt-6">
                  <Link
                    to="/paywall"
                    className="w-full py-3 px-4 rounded-xl bg-gradient-to-r from-amber-500 to-orange-500 hover:from-amber-400 hover:to-orange-400 text-slate-950 font-black text-xs uppercase tracking-wider flex items-center justify-center gap-2 shadow-lg shadow-amber-500/20 transition-all"
                  >
                    <Zap size={14} />
                    {t('subscriptionTab.upgradeCta')}
                  </Link>
                </div>
              </div>

              {/* Capabilities Checklist */}
              <div className="md:col-span-2 rounded-2xl bg-slate-950/60 border border-slate-800 p-6 space-y-4">
                <h3 className="text-sm font-bold text-white uppercase tracking-wider">{t('subscriptionTab.capabilitiesTitle')}</h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
                  <div className="flex items-center gap-2.5 text-xs text-slate-200">
                    <CheckCircle2 size={16} className="text-emerald-400 shrink-0" />
                    <span>{t('subscriptionTab.capabilities.unlimitedQuiz')}</span>
                  </div>
                  <div className="flex items-center gap-2.5 text-xs text-slate-200">
                    <CheckCircle2 size={16} className={isVip ? 'text-emerald-400 shrink-0' : 'text-slate-600 shrink-0'} />
                    <span className={isVip ? 'text-slate-200' : 'text-slate-500 line-through'}>
                      {t('subscriptionTab.capabilities.adaptiveIrt')}
                    </span>
                  </div>
                  <div className="flex items-center gap-2.5 text-xs text-slate-200">
                    <CheckCircle2 size={16} className={isVip ? 'text-emerald-400 shrink-0' : 'text-slate-600 shrink-0'} />
                    <span className={isVip ? 'text-slate-200' : 'text-slate-500 line-through'}>
                      {t('subscriptionTab.capabilities.secureExamRoom')}
                    </span>
                  </div>
                  <div className="flex items-center gap-2.5 text-xs text-slate-200">
                    <CheckCircle2 size={16} className={isVip ? 'text-emerald-400 shrink-0' : 'text-slate-600 shrink-0'} />
                    <span className={isVip ? 'text-slate-200' : 'text-slate-500 line-through'}>
                      {t('subscriptionTab.capabilities.arenaGameshow')}
                    </span>
                  </div>
                  <div className="flex items-center gap-2.5 text-xs text-slate-200">
                    <CheckCircle2 size={16} className="text-emerald-400 shrink-0" />
                    <span>{t('subscriptionTab.capabilities.examHistory')}</span>
                  </div>
                  <div className="flex items-center gap-2.5 text-xs text-slate-200">
                    <CheckCircle2 size={16} className={isVip ? 'text-emerald-400 shrink-0' : 'text-slate-600 shrink-0'} />
                    <span className={isVip ? 'text-slate-200' : 'text-slate-500 line-through'}>
                      {t('subscriptionTab.capabilities.prioritySupport')}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* ── TAB 3: BẢO MẬT & ĐỔI MẬT KHẨU ─────────────────────────────── */}
        {activeTab === 'security' && (
          <div className="space-y-6">
            {/* Google Authenticator (TOTP) 2FA Section */}
            <TwoFactorProfileSection user={currentUser || user} onProfileUpdated={fetchMe} />

            <div className="bg-slate-900/60 border border-slate-800 rounded-3xl p-6 sm:p-8 backdrop-blur-xl shadow-xl space-y-6">
              <div>
                <h2 className="text-xl font-black text-white flex items-center gap-2">
                  <Key size={20} className="text-purple-400" />
                  {t('securityTab.changePasswordTitle')}
                </h2>
                <p className="text-xs text-slate-400 mt-1">{t('securityTab.changePasswordSubtitle')}</p>
              </div>

            {passwordMessage && (
              <div className={`p-4 rounded-xl flex items-center gap-3 text-sm font-medium border ${
                passwordMessage.type === 'success'
                  ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400'
                  : 'bg-rose-500/10 border-rose-500/20 text-rose-400'
              }`}>
                {passwordMessage.type === 'success' ? <CheckCircle2 size={18} /> : <AlertCircle size={18} />}
                {passwordMessage.text}
              </div>
            )}

            <form onSubmit={handleChangePassword} className="space-y-6 max-w-xl">
              <div>
                <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-2">
                  {t('securityTab.currentPassword')}
                </label>
                <div className="relative">
                  <input
                    type={showOldPassword ? 'text' : 'password'}
                    value={oldPassword}
                    onChange={(e) => setOldPassword(e.target.value)}
                    required
                    placeholder="••••••••••••"
                    className="w-full bg-slate-950 border border-slate-800 focus:border-purple-500 rounded-xl px-4 py-3 text-white text-sm focus:outline-none transition-all pr-12 font-mono"
                  />
                  <button
                    type="button"
                    onClick={() => setShowOldPassword(!showOldPassword)}
                    className="absolute right-3.5 top-3.5 text-slate-500 hover:text-slate-300"
                  >
                    {showOldPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                  </button>
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-2">
                  {t('securityTab.newPassword')}
                </label>
                <div className="relative">
                  <input
                    type={showNewPassword ? 'text' : 'password'}
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    required
                    placeholder="••••••••••••"
                    className="w-full bg-slate-950 border border-slate-800 focus:border-purple-500 rounded-xl px-4 py-3 text-white text-sm focus:outline-none transition-all pr-12 font-mono"
                  />
                  <button
                    type="button"
                    onClick={() => setShowNewPassword(!showNewPassword)}
                    className="absolute right-3.5 top-3.5 text-slate-500 hover:text-slate-300"
                  >
                    {showNewPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                  </button>
                </div>

                {/* Password Strength Indicator */}
                {newPassword && (
                  <div className="mt-2 space-y-1.5">
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-slate-400">{t('securityTab.strengthLabel')}</span>
                      <span className={`font-bold ${strengthInfo.text}`}>{strengthInfo.label}</span>
                    </div>
                    <div className="h-1.5 w-full bg-slate-800 rounded-full overflow-hidden flex gap-1">
                      {[1, 2, 3, 4, 5].map((level) => (
                        <div
                          key={level}
                          className={`h-full flex-1 transition-all ${
                            passwordScore >= level ? strengthInfo.color : 'bg-slate-800'
                          }`}
                        />
                      ))}
                    </div>
                  </div>
                )}
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-2">
                  {t('securityTab.confirmPassword')}
                </label>
                <input
                  type="password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  required
                  placeholder="••••••••••••"
                  className="w-full bg-slate-950 border border-slate-800 focus:border-purple-500 rounded-xl px-4 py-3 text-white text-sm focus:outline-none transition-all font-mono"
                />
              </div>

              <div className="pt-2">
                <button
                  type="submit"
                  disabled={isChangingPassword}
                  className="px-6 py-3 rounded-xl bg-purple-600 hover:bg-purple-500 text-white font-bold text-sm flex items-center gap-2 shadow-lg shadow-purple-500/25 transition-all cursor-pointer disabled:opacity-50"
                >
                  {isChangingPassword ? <RefreshCw size={16} className="animate-spin" /> : <Lock size={16} />}
                  {isChangingPassword ? t('securityTab.changing') : t('securityTab.changeCta')}
                </button>
              </div>
            </form>

            {/* Active Session & Device Card (NIST & ISO 27001 Standard) */}
            <div className="p-5 rounded-2xl bg-slate-950/60 border border-slate-800/80 space-y-3">
              <div className="flex items-center justify-between">
                <h3 className="text-xs font-bold text-slate-300 uppercase tracking-wider flex items-center gap-1.5">
                  <Smartphone size={14} className="text-blue-400" />
                  {t('securityTab.sessionTitle')}
                </h3>
                <span className="text-[10px] bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 px-2 py-0.5 rounded-md font-mono font-bold">
                  {t('securityTab.sessionActive')}
                </span>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 text-xs">
                <div>
                  <span className="text-slate-500 block">{t('securityTab.browserPlatform')}</span>
                  <span className="text-slate-200 font-mono font-semibold">
                    {navigator.userAgent.includes('Chrome') ? 'Google Chrome' : navigator.userAgent.includes('Firefox') ? 'Mozilla Firefox' : t('securityTab.webBrowser')} ({navigator.platform || t('securityTab.desktop')})
                  </span>
                </div>
                <div>
                  <span className="text-slate-500 block">{t('securityTab.lastLogin')}</span>
                  <span className="text-slate-200 font-mono">
                    {user?.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString(i18n.language) : t('securityTab.lastLoginNow')}
                  </span>
                </div>
                <div>
                  <span className="text-slate-500 block">{t('securityTab.securityStandard')}</span>
                  <span className="text-slate-200 font-semibold text-emerald-400">
                    NIST SP 800-63B / ISO 27001
                  </span>
                </div>
              </div>
            </div>

            {/* Hardware PKI / Security Badges */}
            <div className="pt-4 border-t border-slate-800/80 grid grid-cols-1 sm:grid-cols-3 gap-4">
              <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800/60 flex items-center gap-3">
                <Shield size={22} className="text-emerald-400" />
                <div>
                  <div className="text-xs font-bold text-slate-200">{t('securityTab.badges.pbkdf2Title')}</div>
                  <div className="text-[11px] text-slate-400">{t('securityTab.badges.pbkdf2Desc')}</div>
                </div>
              </div>

              <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800/60 flex items-center gap-3">
                <Key size={22} className="text-blue-400" />
                <div>
                  <div className="text-xs font-bold text-slate-200">{t('securityTab.badges.pkiTitle')}</div>
                  <div className="text-[11px] text-slate-400">{t('securityTab.badges.pkiDesc')}</div>
                </div>
              </div>

              <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800/60 flex items-center gap-3">
                <Building2 size={22} className="text-purple-400" />
                <div>
                  <div className="text-xs font-bold text-slate-200">{t('securityTab.badges.ssoTitle')}</div>
                  <div className="text-[11px] text-slate-400">{t('securityTab.badges.ssoDesc')}</div>
                </div>
              </div>
            </div>
            </div>
          </div>
        )}

      </div>
    </div>
  );
}

export default ProfilePage;
