import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.includes('@')) {
      setError('Please enter a valid email');
      return;
    }
    login(email.trim().toLowerCase());
    navigate('/dashboard');
  };

  return (
    <div style={{
      minHeight: '100vh', background: '#0d0d1a', color: '#eee',
      fontFamily: "'Inter', 'Segoe UI', system-ui, sans-serif",
      display: 'flex', alignItems: 'center', justifyContent: 'center',
    }}>
      <div style={{
        width: '100%', maxWidth: 420, padding: 40,
        background: '#1a1a2e', borderRadius: 16, border: '1px solid #333',
      }}>
        <Link to="/" style={{ textDecoration: 'none' }}>
          <div style={{ fontSize: 28, fontWeight: 800, textAlign: 'center', marginBottom: 8 }}>
            <span style={{ color: '#eee' }}>CME</span>
            <span style={{ color: '#64B5F6' }}>flow</span>
          </div>
        </Link>
        <p style={{ color: '#888', textAlign: 'center', fontSize: 14, marginBottom: 32 }}>
          Sign in to access your dashboard
        </p>

        <form onSubmit={handleSubmit}>
          <label style={{ display: 'block', color: '#aaa', fontSize: 12, marginBottom: 6 }}>
            Email address
          </label>
          <input
            type="email"
            value={email}
            onChange={e => { setEmail(e.target.value); setError(''); }}
            placeholder="you@example.com"
            style={{
              width: '100%', padding: '12px 14px', fontSize: 14,
              background: '#0d0d1a', border: '1px solid #444', borderRadius: 8,
              color: '#eee', outline: 'none', marginBottom: 16,
            }}
            autoFocus
          />

          {error && (
            <div style={{ color: '#ef4444', fontSize: 12, marginBottom: 12 }}>{error}</div>
          )}

          <button
            type="submit"
            style={{
              width: '100%', padding: '12px 0', fontSize: 15, fontWeight: 600,
              background: 'linear-gradient(135deg, #64B5F6, #42A5F5)',
              color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer',
              marginBottom: 16,
            }}
          >
            Continue to Dashboard
          </button>
        </form>

        <p style={{ color: '#666', fontSize: 12, textAlign: 'center' }}>
          Don't have an account? <Link to="/#waitlist" style={{ color: '#64B5F6' }}>Join the waitlist</Link>
        </p>
      </div>
    </div>
  );
}
