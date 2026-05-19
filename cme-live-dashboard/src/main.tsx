import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';

const style = document.createElement('style');
style.textContent = `
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body { background: #0d0d1a; }
  html { scroll-behavior: smooth; }
  ::-webkit-scrollbar { width: 6px; }
  ::-webkit-scrollbar-track { background: #1a1a2e; }
  ::-webkit-scrollbar-thumb { background: #444; border-radius: 3px; }

  /* Landing page responsive overrides */
  @media (max-width: 768px) {
    .landing-grid-2 { grid-template-columns: 1fr !important; }
    .landing-grid-3 { grid-template-columns: 1fr !important; }
    .landing-hero-grid { grid-template-columns: 1fr !important; gap: 32px !important; }
    .landing-hero h1 { font-size: 32px !important; }
    .landing-hero .hero-sub { font-size: 15px !important; }
    .landing-proof { flex-direction: column !important; gap: 16px !important; }
    .landing-nav-links { display: none !important; }
    .landing-nav-mobile { display: flex !important; }
    .landing-section-title { font-size: 26px !important; }
    .landing-pricing-grid { grid-template-columns: 1fr !important; max-width: 400px; margin: 0 auto; }
    .landing-formula { font-size: 14px !important; flex-wrap: wrap; }
    .landing-bands-grid { grid-template-columns: repeat(3, 1fr) !important; }
    .landing-vernik-row { flex-direction: column !important; gap: 12px !important; }
    .landing-cta-row { flex-direction: column !important; gap: 8px !important; }
    .landing-cta-row a { text-align: center; }
    .landing-waitlist-row { flex-direction: column !important; }
    .landing-waitlist-row input { width: 100% !important; }
    .landing-waitlist-row button { width: 100% !important; }
  }

  @media (max-width: 480px) {
    .landing-bands-grid { grid-template-columns: repeat(2, 1fr) !important; }
    .landing-hero h1 { font-size: 26px !important; }
  }
`;
document.head.appendChild(style);

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </React.StrictMode>,
);
