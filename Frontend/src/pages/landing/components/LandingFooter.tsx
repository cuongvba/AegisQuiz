/**
 * AegisQuiz — LandingFooter (Landing Page)
 * ===========================================
 * Footer đầy đủ: sitemap, liên hệ, ngôn ngữ (lần 2), bản quyền.
 * FSD Layer: pages/landing/components/
 */
import { Bot } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useTenant } from '@/app/providers/TenantProvider';
import { LanguageSwitcher } from '@/widgets/app-header/LanguageSwitcher';

export function LandingFooter() {
  const { t } = useTranslation('landing');
  const { tenantName } = useTenant();
  const year = new Date().getFullYear();

  const columns = [
    { title: t('footer.product'), links: [
      { label: t('footer.links.features'),  href: '#features' },
      { label: t('footer.links.gameshows'), href: '#gameshows' },
      { label: t('footer.links.pricing'),   href: '#pricing' },
    ]},
    { title: t('footer.company'), links: [
      { label: t('footer.links.about'),   href: '#' },
      { label: t('footer.links.contact'), href: '#' },
    ]},
    { title: t('footer.legal'), links: [
      { label: t('footer.links.privacy'), href: '#' },
      { label: t('footer.links.terms'),   href: '#' },
    ]},
  ];

  return (
    <footer className="border-t border-slate-800/80 bg-slate-950">
      <div className="max-w-6xl mx-auto px-4 sm:px-6 py-12 grid sm:grid-cols-2 lg:grid-cols-5 gap-10">
        {/* Brand column */}
        <div className="lg:col-span-2 space-y-3">
          <div className="flex items-center gap-2.5">
            <div
              className="w-8 h-8 rounded-full flex items-center justify-center border-2 border-white shadow-md flex-shrink-0"
              style={{ background: 'var(--tenant-brand-gradient)' }}
              aria-hidden="true"
            >
              <Bot className="h-4.5 w-4.5 text-white" />
            </div>
            <span className="text-base font-extrabold text-white">{tenantName}</span>
          </div>
          <p className="text-sm text-slate-400 max-w-xs leading-relaxed">{t('footer.tagline')}</p>
          <LanguageSwitcher />
        </div>

        {/* Link columns */}
        {columns.map((col) => (
          <div key={col.title}>
            <p className="text-xs font-black uppercase tracking-wider text-slate-500 mb-3">{col.title}</p>
            <ul className="space-y-2">
              {col.links.map((link) => (
                <li key={link.label}>
                  <a href={link.href} className="text-sm text-slate-400 hover:text-white transition-colors cursor-pointer">
                    {link.label}
                  </a>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>

      <div className="border-t border-slate-800/80 py-5">
        <p className="text-center text-xs text-slate-600">
          © {year} {tenantName}. {t('footer.copyright')}
        </p>
      </div>
    </footer>
  );
}
