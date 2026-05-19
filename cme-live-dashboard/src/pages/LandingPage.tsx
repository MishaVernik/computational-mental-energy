import React from 'react';
import { LandingNav } from '../components/landing/LandingNav';
import { HeroSection } from '../components/landing/HeroSection';
import { WhatIsMentalEnergy } from '../components/landing/WhatIsMentalEnergy';
import { WhatIsFlowState } from '../components/landing/WhatIsFlowState';
import { HowWeMeasure } from '../components/landing/HowWeMeasure';
import { PricingSection } from '../components/landing/PricingSection';
import { WaitlistForm } from '../components/landing/WaitlistForm';
import { Footer } from '../components/landing/Footer';
import { ScrollReveal } from '../components/landing/ScrollReveal';

export default function LandingPage() {
  return (
    <div style={{
      minHeight: '100vh', background: '#0d0d1a', color: '#eee',
      fontFamily: "'Inter', 'Segoe UI', system-ui, -apple-system, sans-serif",
      overflowX: 'hidden',
    }}>
      <LandingNav />
      <HeroSection />
      <ScrollReveal>
        <WhatIsMentalEnergy />
      </ScrollReveal>
      <ScrollReveal>
        <WhatIsFlowState />
      </ScrollReveal>
      <ScrollReveal>
        <HowWeMeasure />
      </ScrollReveal>
      <ScrollReveal>
        <PricingSection />
      </ScrollReveal>
      <ScrollReveal>
        <WaitlistForm />
      </ScrollReveal>
      <Footer />
    </div>
  );
}
