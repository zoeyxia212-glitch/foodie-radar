import { useState, type FormEvent } from 'react';
import { api } from '../api';
import { useAuth } from '../auth';

export default function AuthModal({ origins }: { origins: string[] }) {
  const { signIn, closeLogin } = useAuth();
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [origin, setOrigin] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const res =
        mode === 'login'
          ? await api.login({ email, password })
          : await api.register({ email, password, displayName, origin });
      signIn(res);
    } catch (err) {
      setError(err instanceof Error ? err.message : '出错了');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="mask" onClick={(e) => e.target === e.currentTarget && closeLogin()}>
      <form className="modal auth" onSubmit={submit} role="dialog" aria-modal="true">
        <div className="modal-head">
          <h2>{mode === 'login' ? '登录' : '注册'}</h2>
          <button type="button" className="close" onClick={closeLogin} aria-label="关闭">×</button>
        </div>

        <input type="email" placeholder="邮箱" value={email} onChange={(e) => setEmail(e.target.value)} required autoComplete="email" />
        <input
          type="password"
          placeholder={mode === 'login' ? '密码' : '密码（至少 8 位）'}
          value={password}
          minLength={mode === 'register' ? 8 : undefined}
          onChange={(e) => setPassword(e.target.value)}
          required
          autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
        />

        {mode === 'register' && (
          <>
            <input placeholder="昵称（会显示在评价上）" value={displayName} maxLength={50} onChange={(e) => setDisplayName(e.target.value)} required />
            <select value={origin} onChange={(e) => setOrigin(e.target.value)} required>
              <option value="">我来自…（用于「同乡评价」）</option>
              {origins.map((o) => (
                <option key={o} value={o}>{o}</option>
              ))}
            </select>
          </>
        )}

        {error && <div className="error">{error}</div>}

        <button type="submit" className="primary wide" disabled={busy}>
          {busy ? '请稍候…' : mode === 'login' ? '登录' : '注册并登录'}
        </button>

        <button type="button" className="link" onClick={() => { setMode(mode === 'login' ? 'register' : 'login'); setError(null); }}>
          {mode === 'login' ? '还没有账号？注册' : '已有账号？登录'}
        </button>
      </form>
    </div>
  );
}
