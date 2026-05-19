import React, { useState } from 'react';
import { Link } from 'react-router-dom';

const navLinks = [
  { href: '#mental-energy', label: 'Mental Energy' },
  { href: '#flow-state', label: 'Flow State' },
  { href: '#how-it-works', label: 'How it works' },
  { href: '#pricing', label: 'Pricing' },
];

export const LandingNav: React.FC = () => {
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <nav style={{
      position: 'fixed', top: 0, left: 0, right: 0, zIndex: 100,
      background: 'rgba(13, 13, 26, 0.85)', backdropFilter: 'blur(12px)',
      borderBottom: '1px solid rgba(100, 181, 246, 0.1)',
    }}>
      <div style={{
        maxWidth: 1200, margin: '0 auto', padding: '14px 24px',
        display: 'flex', justifyContent: 'space-between', alignItems: 'center',
      }}>
        <a href="#" style={{ textDecoration: 'none', fontSize: 22, fontWeight: 800 }}>
          <span style={{ color: '#eee' }}>CME</span>
          <span style={{ color: '#64B5F6' }}>flow</span>
        </a>

        {/* Desktop links */}
        <div className="landing-nav-links" style={{ display: 'flex', gap: 28, alignItems: 'center' }}>
          {navLinks.map(l => (
            <a key={l.href} href={l.href} style={{
              color: '#aaa', textDecoration: 'none', fontSize: 14, fontWeight: 500,
              transition: 'color 0.2s',
            }}
              onMouseEnter={e => (e.currentTarget.style.color = '#eee')}
              onMouseLeave={e => (e.currentTarget.style.color = '#aaa')}
            >{l.label}</a>
          ))}
          <Link to="/login" style={{
            padding: '8px 20px', fontSize: 13, fontWeight: 600,
            background: 'linear-gradient(135deg, #64B5F6, #42A5F5)',
            color: '#fff', borderRadius: 8, textDecoration: 'none',
          }}>Sign In</Link>
        </div>

        {/* Mobile hamburger */}
        <button
          className="landing-nav-mobile"
          onClick={() => setMobileOpen(!mobileOpen)}
          style={{
            display: 'none', background: 'none', border: 'none',
            color: '#eee', fontSize: 24, cursor: 'pointer', padding: 4,
          }}
          aria-label="Menu"
        >
          {mobileOpen ? '\u2715' : '\u2630'}
        </button>
      </div>

      {/* Mobile dropdown */}
      {mobileOpen && (
        <div style={{
          background: 'rgba(13, 13, 26, 0.95)', padding: '12px 24px 20px',
          borderBottom: '1px solid #333',
          display: 'flex', flexDirection: 'column', gap: 12,
        }}>
          {navLinks.map(l => (
            <a key={l.href} href={l.href}
              onClick={() => setMobileOpen(false)}
              style={{ color: '#ccc', textDecoration: 'none', fontSize: 15, padding: '6px 0' }}
            >{l.label}</a>
          ))}
          <Link to="/login" onClick={() => setMobileOpen(false)} style={{
            padding: '10px 20px', fontSize: 14, fontWeight: 600, textAlign: 'center',
            background: 'linear-gradient(135deg, #64B5F6, #42A5F5)',
            color: '#fff', borderRadius: 8, textDecoration: 'none', marginTop: 4,
          }}>Sign In</Link>
        </div>
      )}
    </nav>
  );
};
