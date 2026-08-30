import { FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, ApiError } from '../api/client';

export function AdminLoginPage() {
  const navigate = useNavigate();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await api.login(username, password);
      navigate('/admin');
    } catch (err) {
      setError((err as ApiError).message || 'Не удалось войти.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="adminLoginPage">
      <form className="adminLoginForm" onSubmit={submit}>
        <button
          className="iconButton closeButton"
          type="button"
          onClick={() => navigate('/gifts')}
          aria-label="Закрыть окно входа"
        >
          ×
        </button>
        <h1>Вход администратора</h1>
        <label className="fieldLabel">
          Логин
          <input value={username} onChange={(event) => setUsername(event.target.value)} autoComplete="username" required />
        </label>
        <label className="fieldLabel">
          Пароль
          <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} autoComplete="current-password" required />
        </label>
        {error && <p className="stateText errorText">{error}</p>}
        <button className="primaryButton" type="submit" disabled={busy}>Войти</button>
      </form>
    </main>
  );
}
