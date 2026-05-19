import React, { useEffect, useRef } from 'react';

const gradientText: React.CSSProperties = {
  background: 'linear-gradient(135deg, #64B5F6, #7C4DFF)',
  WebkitBackgroundClip: 'text',
  WebkitTextFillColor: 'transparent',
};

const bands = [
  { name: 'Delta', range: '1\u20134 Hz', meaning: 'Deep sleep', color: '#6366f1', amplitude: 24, freq: 0.8, speed: 0.015 },
  { name: 'Theta', range: '4\u20138 Hz', meaning: 'Meditation, memory', color: '#8b5cf6', amplitude: 20, freq: 1.5, speed: 0.025 },
  { name: 'Alpha', range: '8\u201313 Hz', meaning: 'Relaxed focus, calm', color: '#10b981', amplitude: 28, freq: 2.5, speed: 0.04 },
  { name: 'Beta', range: '13\u201330 Hz', meaning: 'Active thinking, analysis', color: '#f59e0b', amplitude: 16, freq: 5, speed: 0.07 },
  { name: 'Gamma', range: '30\u201345 Hz', meaning: 'Peak concentration', color: '#ef4444', amplitude: 12, freq: 9, speed: 0.12 },
];

const AnimatedWave: React.FC<{ color: string; amplitude: number; freq: number; speed: number }> = ({ color, amplitude, freq, speed }) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const phaseRef = useRef(0);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let animId: number;
    const w = 100;
    const h = 70;
    canvas.width = w * 2;
    canvas.height = h * 2;

    const draw = () => {
      ctx.clearRect(0, 0, w * 2, h * 2);
      ctx.beginPath();
      ctx.strokeStyle = color;
      ctx.lineWidth = 3;
      ctx.lineCap = 'round';

      for (let x = 0; x <= w * 2; x++) {
        const normalX = x / (w * 2);
        const y = h + amplitude * 1.2 * Math.sin(normalX * Math.PI * 2 * freq + phaseRef.current);
        if (x === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      }
      ctx.stroke();

      phaseRef.current += speed;
      animId = requestAnimationFrame(draw);
    };

    draw();
    return () => cancelAnimationFrame(animId);
  }, [color, amplitude, freq, speed]);

  return (
    <canvas
      ref={canvasRef}
      style={{ width: 100, height: 70, display: 'block', margin: '0 auto', opacity: 0.85 }}
    />
  );
};

const pipelineSteps = [
  {
    num: '1',
    title: 'Your brainwaves tell a story',
    desc: 'A lightweight EEG headband (like Muse) reads electrical activity from 4 points on your scalp. Every 5 seconds, we capture a snapshot of 5 frequency bands across all channels \u2014 that\'s 20 data points per window.',
  },
  {
    num: '2',
    title: 'We calculate the energy',
    desc: 'We multiply your brain\'s electrical activity by how hard the task is and whether you\'re in flow. Higher brainwave power + harder task + deeper flow = more Verniks per second. The formula is patented and peer-reviewed.',
  },
  {
    num: '3',
    title: 'AI detects your flow state',
    desc: 'A quantum-classical hybrid classifier reads your brainwave patterns 12 times per minute and estimates how close you are to flow. This feeds back into the energy calculation, making it more accurate than brainwave power alone.',
  },
];

export const HowWeMeasure: React.FC = () => (
  <section id="how-it-works" style={{ padding: '80px 0' }}>
    <div style={{ maxWidth: 1100, margin: '0 auto', padding: '0 24px' }}>

      <h2 style={{ fontSize: 36, fontWeight: 800, textAlign: 'center', marginBottom: 16, color: '#eee' }}>
        How We <span style={gradientText}>Measure</span> It
      </h2>

      <p style={{
        maxWidth: 700, margin: '0 auto 48px', textAlign: 'center',
        fontSize: 17, color: '#999', lineHeight: 1.7,
      }}>
        No lab required. A $200 headband, your phone, and our software.
        Here&rsquo;s what happens under the hood.
      </p>

      {/* Brainwave bands visual */}
      <div style={{
        background: '#111827', borderRadius: 16, padding: 32,
        border: '1px solid #1f2937', marginBottom: 40,
      }}>
        <h3 style={{ color: '#eee', fontSize: 18, fontWeight: 700, marginBottom: 8, textAlign: 'center' }}>
          Your brain produces 5 types of waves
        </h3>
        <p style={{ color: '#888', fontSize: 13, textAlign: 'center', marginBottom: 24 }}>
          Each frequency band reflects a different kind of mental activity
        </p>
        <div className="landing-bands-grid" style={{
          display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: 16,
        }}>
          {bands.map(b => (
            <div key={b.name} style={{ textAlign: 'center' }}>
              <AnimatedWave color={b.color} amplitude={b.amplitude} freq={b.freq} speed={b.speed} />
              <div style={{ color: b.color, fontSize: 15, fontWeight: 700, marginTop: 4 }}>{b.name}</div>
              <div style={{ color: '#999', fontSize: 12, marginBottom: 2 }}>{b.range}</div>
              <div style={{ color: '#666', fontSize: 11 }}>{b.meaning}</div>
            </div>
          ))}
        </div>
      </div>

      {/* Three pipeline steps */}
      <div className="landing-grid-3" style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 20, marginBottom: 40 }}>
        {pipelineSteps.map(s => (
          <div key={s.num} style={{
            background: '#111827', borderRadius: 16, padding: 28,
            border: '1px solid #1f2937',
          }}>
            <div style={{
              width: 40, height: 40, borderRadius: '50%', marginBottom: 16,
              background: 'linear-gradient(135deg, #64B5F6, #42A5F5)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              fontSize: 18, fontWeight: 800, color: '#fff',
            }}>{s.num}</div>
            <h3 style={{ fontSize: 17, fontWeight: 700, color: '#eee', marginBottom: 10 }}>{s.title}</h3>
            <p style={{ color: '#999', fontSize: 14, lineHeight: 1.6 }}>{s.desc}</p>
          </div>
        ))}
      </div>

      {/* Simplified formula */}
      <div style={{
        background: '#111827', borderRadius: 12, padding: '24px 32px',
        border: '1px solid #1f2937', maxWidth: 800, margin: '0 auto',
        textAlign: 'center',
      }}>
        <div style={{ color: '#888', fontSize: 13, marginBottom: 12 }}>The CME formula, simplified:</div>
        <div className="landing-formula" style={{ fontSize: 20, fontWeight: 600, color: '#eee', marginBottom: 12 }}>
          <span style={{ color: '#64B5F6' }}>Mental Energy</span>
          {' = '}
          <span style={{ color: '#f59e0b' }}>Brain Activity</span>
          {' \u00D7 '}
          <span style={{ color: '#8b5cf6' }}>Task Difficulty</span>
          {' \u00D7 '}
          <span style={{ color: '#10b981' }}>Flow Factor</span>
          {' \u00D7 '}
          <span style={{ color: '#999' }}>Time</span>
        </div>
        <div style={{ color: '#666', fontSize: 12, lineHeight: 1.6 }}>
          Brain Activity = weighted sum of your 5 brainwave bands &nbsp;|&nbsp;
          Task Difficulty = what you told us you&rsquo;re doing (0 to 1) &nbsp;|&nbsp;
          Flow Factor = how close you are to flow (detected by AI) &nbsp;|&nbsp;
          Time = 5-second window
        </div>
      </div>

      {/* Bottom trust line */}
      <p style={{
        textAlign: 'center', color: '#666', fontSize: 14, marginTop: 32,
        fontStyle: 'italic',
      }}>
        The science is published, patented, and validated on real IBM Quantum hardware.
        But you don&rsquo;t need to understand any of that &mdash;
        just wear the headband and watch your dashboard.
      </p>
    </div>
  </section>
);
