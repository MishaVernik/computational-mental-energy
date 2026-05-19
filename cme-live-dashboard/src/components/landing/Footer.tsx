import React from 'react';

export const Footer: React.FC = () => (
  <footer style={{
    padding: '40px 0', borderTop: '1px solid #1f2937',
  }}>
    <div style={{
      maxWidth: 1200, margin: '0 auto', padding: '0 24px',
      display: 'flex', justifyContent: 'space-between', alignItems: 'center',
      flexWrap: 'wrap', gap: 16,
    }}>
      <div>
        <div style={{ fontSize: 20, fontWeight: 800, marginBottom: 4 }}>
          <span style={{ color: '#eee' }}>CME</span>
          <span style={{ color: '#64B5F6' }}>flow</span>
        </div>
        <div style={{ color: '#666', fontSize: 12 }}>Measure your mental energy.</div>
      </div>
      <div style={{ display: 'flex', gap: 24 }}>
        <a href="#science" style={{ color: '#888', textDecoration: 'none', fontSize: 13 }}>Science</a>
        <a href="#pricing" style={{ color: '#888', textDecoration: 'none', fontSize: 13 }}>Pricing</a>
        <a href="mailto:hello@cmeflow.com" style={{ color: '#888', textDecoration: 'none', fontSize: 13 }}>Contact</a>
      </div>
      <div style={{ color: '#555', fontSize: 11 }}>
        &copy; 2026 CMEflow. Patent pending. Vernik (Vn) is a registered measurement unit.
      </div>
    </div>
  </footer>
);
