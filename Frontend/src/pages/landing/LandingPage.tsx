/**
 * AegisQuiz — LandingPage (Public Marketing Homepage)
 * ======================================================
 * Mặt tiền công khai duy nhất của hệ thống, KHÔNG yêu cầu đăng nhập.
 * Route: "/" (thay thế Dashboard — Dashboard đã chuyển sang "/dashboard").
 *
 * Khác với MainLayout (dùng cho sản phẩm sau đăng nhập): trang này dùng
 * layout riêng nhẹ hơn (LandingHeader/LandingFooter), tối ưu tốc độ tải &
 * chuyển đổi (conversion) thay vì điều hướng sản phẩm.
 *
 * FSD Layer: pages/landing/
 */
import { LandingHeader }     from './components/LandingHeader';
import { HeroSection }       from './components/HeroSection';
import { SocialProofSection } from './components/SocialProofSection';
import { FeaturesSection }   from './components/FeaturesSection';
import { GameshowSection }   from './components/GameshowSection';
import { SegmentsSection }   from './components/SegmentsSection';
import { PricingSection }    from './components/PricingSection';
import { TrustSection }      from './components/TrustSection';
import { FinalCtaSection }   from './components/FinalCtaSection';
import { LandingFooter }     from './components/LandingFooter';

export function LandingPage() {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 font-sans overflow-x-hidden">
      <LandingHeader />
      <main id="main-content">
        <HeroSection />
        <SocialProofSection />
        <FeaturesSection />
        <GameshowSection />
        <SegmentsSection />
        <PricingSection />
        <TrustSection />
        <FinalCtaSection />
      </main>
      <LandingFooter />
    </div>
  );
}
