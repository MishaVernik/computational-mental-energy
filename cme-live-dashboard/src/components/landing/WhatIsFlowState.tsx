import React from 'react';

const gradientText: React.CSSProperties = {
  background: 'linear-gradient(135deg, #64B5F6, #7C4DFF)',
  WebkitBackgroundClip: 'text',
  WebkitTextFillColor: 'transparent',
};

const examples = [
  { activity: 'Coding', desc: 'You start a feature. Three hours vanish. The code just... wrote itself.' },
  { activity: 'Meditation', desc: 'Your thoughts quiet down. The world feels still. Time stretches.' },
  { activity: 'Sports', desc: 'Every move is automatic. You don\'t think – you just perform.' },
  { activity: 'Writing', desc: 'Words pour out faster than you can type. The page fills itself.' },
];

const flowZoneLevels = [
  { label: 'Anxiety', y: 90, color: '#ef4444', desc: 'Task too hard, skills too low' },
  { label: 'Flow', y: 50, color: '#64B5F6', desc: 'Perfect balance: challenge matches skill' },
  { label: 'Boredom', y: 10, color: '#666', desc: 'Task too easy, mind wanders' },
];

export const WhatIsFlowState: React.FC = () => (
  <section id="flow-state" style={{ padding: '80px 0', background: '#0a0a18' }}>
    <div style={{ maxWidth: 1100, margin: '0 auto', padding: '0 24px' }}>

      <h2 style={{ fontSize: 36, fontWeight: 800, textAlign: 'center', marginBottom: 16, color: '#eee' }}>
        What is <span style={gradientText}>Flow State</span>?
      </h2>

      <p style={{
        maxWidth: 720, margin: '0 auto 48px', textAlign: 'center',
        fontSize: 17, color: '#999', lineHeight: 1.7,
      }}>
        Flow is that feeling when time disappears. You&rsquo;re coding and suddenly 3 hours passed.
        You&rsquo;re meditating and the world goes quiet. Psychologist
        <strong style={{ color: '#ccc' }}> Mihaly Csikszentmihalyi </strong>
        identified it in 1975 &mdash; a state of complete absorption where you perform at your peak
        while using <em>less</em> mental energy per unit of output.
      </p>

      <div className="landing-grid-2" style={{
        display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 48,
        marginBottom: 48, alignItems: 'start',
      }}>
        {/* Left: what flow feels like */}
        <div>
          <h3 style={{ fontSize: 20, fontWeight: 700, color: '#eee', marginBottom: 20 }}>
            You already know flow. You just can&rsquo;t see it &mdash; yet.
          </h3>

          <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            {examples.map(ex => (
              <div key={ex.activity} style={{
                background: '#111827', borderRadius: 12, padding: '16px 20px',
                border: '1px solid #1f2937',
              }}>
                <div style={{ color: '#64B5F6', fontSize: 13, fontWeight: 700, marginBottom: 4 }}>
                  {ex.activity}
                </div>
                <div style={{ color: '#bbb', fontSize: 14, lineHeight: 1.5, fontStyle: 'italic' }}>
                  &ldquo;{ex.desc}&rdquo;
                </div>
              </div>
            ))}
          </div>

          <p style={{ color: '#999', fontSize: 14, lineHeight: 1.7, marginTop: 20 }}>
            During flow, your brain shifts into a distinct electrical pattern.
            <strong style={{ color: '#ccc' }}> Alpha waves </strong> (relaxed focus) increase.
            <strong style={{ color: '#ccc' }}> Theta waves </strong> (deep processing) rise.
            The ratio between them changes in a measurable way.
            That&rsquo;s what CMEflow detects &mdash; not a feeling, but a brainwave signature.
          </p>
        </div>

        {/* Right: Csikszentmihalyi flow model */}
        <div style={{
          background: '#111827', borderRadius: 16, padding: 32,
          border: '1px solid #1f2937',
        }}>
          <h4 style={{ color: '#eee', fontSize: 15, fontWeight: 700, textAlign: 'center', marginBottom: 20 }}>
            The Csikszentmihalyi Flow Model
          </h4>

          <svg viewBox="0 0 400 340" style={{ width: '100%', maxWidth: 400, display: 'block', margin: '0 auto 20px' }}>
            {/* Axes */}
            <line x1="50" y1="290" x2="50" y2="20" stroke="#444" strokeWidth="1.5" />
            <line x1="50" y1="290" x2="380" y2="290" stroke="#444" strokeWidth="1.5" />
            {/* Arrowheads */}
            <polygon points="50,20 46,30 54,30" fill="#444" />
            <polygon points="380,290 370,286 370,294" fill="#444" />
            {/* Axis labels */}
            <text x="15" y="160" fill="#888" fontSize="12" textAnchor="middle" transform="rotate(-90,15,160)">Challenge</text>
            <text x="215" y="320" fill="#888" fontSize="12" textAnchor="middle">Skill</text>
            {/* Low/High labels */}
            <text x="60" y="305" fill="#666" fontSize="10">Low</text>
            <text x="355" y="305" fill="#666" fontSize="10">High</text>
            <text x="55" y="280" fill="#666" fontSize="10">Low</text>
            <text x="55" y="35" fill="#666" fontSize="10">High</text>

            {/* Anxiety zone: top-left triangle */}
            <path d="M 55,25 L 55,200 L 180,25 Z" fill="rgba(239,68,68,0.08)" />
            <text x="72" y="60" fill="#ef4444" fontSize="13" fontWeight="700" opacity="0.9">Anxiety</text>
            <text x="72" y="74" fill="#888" fontSize="9">High challenge,</text>
            <text x="72" y="85" fill="#888" fontSize="9">low skill</text>

            {/* Boredom zone: bottom-right triangle */}
            <path d="M 220,285 L 375,285 L 375,130 Z" fill="rgba(100,100,100,0.06)" />
            <text x="295" y="250" fill="#999" fontSize="13" fontWeight="700" opacity="0.8">Boredom</text>
            <text x="295" y="264" fill="#666" fontSize="9">Low challenge,</text>
            <text x="295" y="275" fill="#666" fontSize="9">high skill</text>

            {/* Flow channel: smooth diagonal band from center to top-right */}
            <path
              d="M 140,285 Q 120,240 140,190 Q 165,120 300,38 Q 340,22 375,75 Q 355,120 200,240 Q 170,272 140,285 Z"
              fill="rgba(100,181,246,0.10)"
              stroke="rgba(100,181,246,0.30)"
              strokeWidth="1.5"
            />
            {/* Inner glow */}
            <path
              d="M 165,265 Q 150,225 165,180 Q 185,125 310,48 Q 335,40 358,82 Q 340,120 215,230 Q 190,258 165,265 Z"
              fill="rgba(100,181,246,0.06)"
            />
            <text x="270" y="115" fill="#64B5F6" fontSize="22" fontWeight="800" textAnchor="middle">FLOW</text>
            <text x="270" y="138" fill="#64B5F6" fontSize="11" textAnchor="middle" opacity="0.8">Challenge matches skill</text>

            {/* Diagonal guide line */}
            <line x1="50" y1="290" x2="375" y2="25" stroke="rgba(100,181,246,0.2)" strokeWidth="1" strokeDasharray="6,4" />
          </svg>

          <p style={{ color: '#999', fontSize: 13, lineHeight: 1.6, textAlign: 'center' }}>
            Flow happens when the challenge of a task closely matches your skill level.
            Too easy and you&rsquo;re bored. Too hard and you&rsquo;re anxious.
            In the sweet spot, your brain enters a state of effortless concentration
            &mdash; and your brainwaves change in a way we can detect.
          </p>
        </div>
      </div>

      {/* Bottom callout */}
      <div style={{
        background: '#111827', borderRadius: 12, padding: '20px 28px',
        border: '1px solid rgba(100,181,246,0.2)',
        display: 'flex', alignItems: 'center', gap: 20, maxWidth: 800, margin: '0 auto',
      }}>
        <div style={{ fontSize: 36, flexShrink: 0 }}>{'\u{1F9E0}'}</div>
        <div>
          <div style={{ color: '#eee', fontSize: 15, fontWeight: 600, marginBottom: 4 }}>
            Why does flow matter for mental energy?
          </div>
          <div style={{ color: '#999', fontSize: 14, lineHeight: 1.6 }}>
            When you&rsquo;re in flow, your brain works <em>more efficiently</em>.
            You produce better output while spending fewer Verniks per second.
            CMEflow tracks both your energy drain <em>and</em> your flow probability,
            so you can find your personal sweet spot for sustainable performance.
          </div>
        </div>
      </div>
    </div>
  </section>
);
