import { NavLink } from 'react-router-dom';
import { useState } from 'react';
import { post } from '../api/apiClient';

export default function NavBar() {
  const [seeding, setSeeding] = useState(false);
  const [seedMessage, setSeedMessage] = useState<string | null>(null);
  const [seedError, setSeedError] = useState(false);

  async function handleSeed() {
    setSeeding(true);
    setSeedMessage(null);
    setSeedError(false);
    try {
      const result = await post<{ message: string }>('/seed', {});
      setSeedMessage(result.message ?? 'Данные сгенерированы');
      setSeedError(false);
    } catch (err) {
      setSeedMessage(err instanceof Error ? err.message : 'Ошибка генерации');
      setSeedError(true);
    } finally {
      setSeeding(false);
      setTimeout(() => setSeedMessage(null), 5000);
    }
  }

  return (
    <nav className="navbar">
      <div className="navbar-brand">Personnel Org</div>

      <ul className="navbar-links">
        <li>
          <NavLink to="/organizations" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            Организации
          </NavLink>
        </li>
        <li>
          <NavLink to="/employees" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            Сотрудники
          </NavLink>
        </li>
        <li>
          <NavLink to="/actionlog" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            Журнал
          </NavLink>
        </li>
      </ul>

      <div className="navbar-actions">
        {seedMessage && (
          <span className="seed-message" style={{ color: seedError ? 'var(--danger)' : 'var(--success)' }}>
            {seedMessage}
          </span>
        )}
        <button className="btn-seed" onClick={handleSeed} disabled={seeding}>
          {seeding ? 'Генерация...' : 'Тестовые данные'}
        </button>
      </div>
    </nav>
  );
}
