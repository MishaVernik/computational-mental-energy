import React, { useState } from 'react';

const API_BASE = window.location.hostname === 'localhost'
  ? 'http://localhost:5000'
  : `http://${window.location.hostname}:5000`;

export const WaitlistForm: React.FC = () => {
  const [email, setEmail] = useState('');
  const [role, setRole] = useState('developer');
  const [hasMuse, setHasMuse] = useState(false);
  const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');
  const [errorMsg, setErrorMsg] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.includes('@')) { setErrorMsg('Please enter a valid email'); return; }
    setStatus('loading');
    try {
      const res = await fetch(`${API_BASE}/api/waitlist`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: email.trim().toLowerCase(), role, hasMuse }),
      });
      if (res.status === 409) {
        setStatus('success');
        return;
      }
      if (!res.ok) throw new Error(await res.text());
      localStorage.setItem('cmeflow_waitlist_email', email);
      setStatus('success');
    } catch (err: any) {
      setErrorMsg(err.message || 'Something went wrong');
      setStatus('error');
    }
  };

  const roles = [
    { value: 'developer', label: 'Developer / Knowledge Worker' },
    { value: 'wellness', label: 'Meditation / Wellness' },
    { value: 'team-lead', label: 'Team Lead / Manager' },
    { value: 'researcher', label: 'Researcher' },
  ];

  if (status === 'success') {
    return (
      <section id="waitlist" style={{ padding: '80px 0', background: '#0a0a18' }}>
        <div style={{ maxWidth: 560, margin: '0 auto', padding: '0 24px', textAlign: 'center' }}>
          <div style={{ fontSize: 48, marginBottom: 16 }}>{'\u2705'}</div>
          <h2 style={{ fontSize: 28, fontWeight: 800, color: '#eee', marginBottom: 12 }}>You're on the list!</h2>
          <p style={{ color: '#999', fontSize: 16 }}>We'll notify you as soon as CMEflow launches.</p>
        </div>
      </section>
    );
  }

  return (
    <section id="waitlist" style={{ padding: '80px 0', background: '#0a0a18' }}>
      <div style={{ maxWidth: 560, margin: '0 auto', padding: '0 24px' }}>
        <h2 style={{ fontSize: 36, fontWeight: 800, textAlign: 'center', marginBottom: 8, color: '#eee' }}>
          Get Early Access
        </h2>
        <p style={{ textAlign: 'center', color: '#888', fontSize: 16, marginBottom: 32 }}>
          Be among the first to measure your mental energy.
        </p>

        <form onSubmit={handleSubmit} style={{
          background: '#111827', borderRadius: 16, padding: 32,
          border: '1px solid #1f2937',
        }}>
          <div className="landing-waitlist-row" style={{ display: 'flex', gap: 10, marginBottom: 20 }}>
            <input
              type="email"
              value={email}
              onChange={e => { setEmail(e.target.value); setErrorMsg(''); setStatus('idle'); }}
              placeholder="your@email.com"
              style={{
                flex: 1, padding: '12px 14px', fontSize: 14,
                background: '#0d0d1a', border: '1px solid #333', borderRadius: 8,
                color: '#eee', outline: 'none',
              }}
            />
            <button type="submit" disabled={status === 'loading'} style={{
              padding: '12px 24px', fontSize: 14, fontWeight: 600,
              background: 'linear-gradient(135deg, #64B5F6, #42A5F5)',
              color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer',
              opacity: status === 'loading' ? 0.6 : 1,
            }}>
              {status === 'loading' ? 'Joining...' : 'Join Waitlist'}
            </button>
          </div>

          <div style={{ marginBottom: 16 }}>
            <div style={{ color: '#888', fontSize: 12, marginBottom: 8 }}>I'm mostly interested as a:</div>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
              {roles.map(r => (
                <label key={r.value} style={{
                  display: 'flex', alignItems: 'center', gap: 6,
                  cursor: 'pointer', fontSize: 13, color: role === r.value ? '#64B5F6' : '#999',
                  padding: '6px 12px', borderRadius: 6,
                  background: role === r.value ? 'rgba(100,181,246,0.1)' : 'transparent',
                  border: `1px solid ${role === r.value ? '#64B5F6' : '#333'}`,
                }}>
                  <input
                    type="radio" name="role" value={r.value}
                    checked={role === r.value}
                    onChange={() => setRole(r.value)}
                    style={{ display: 'none' }}
                  />
                  {r.label}
                </label>
              ))}
            </div>
          </div>

          <label style={{
            display: 'flex', alignItems: 'center', gap: 8,
            cursor: 'pointer', fontSize: 13, color: '#999',
          }}>
            <input
              type="checkbox" checked={hasMuse}
              onChange={e => setHasMuse(e.target.checked)}
              style={{ accentColor: '#64B5F6' }}
            />
            I already own a Muse headband
          </label>

          {errorMsg && (
            <div style={{ color: '#ef4444', fontSize: 12, marginTop: 12 }}>{errorMsg}</div>
          )}
        </form>
      </div>
    </section>
  );
};
