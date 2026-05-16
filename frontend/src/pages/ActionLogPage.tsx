import { useEffect, useState } from 'react';
import { get } from '../api/apiClient';
import ActionLogTable, { ActionLog } from '../components/ActionLogTable';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

interface PagedResult<T> { items: T[]; totalCount: number; page: number; pageSize: number; }

const PAGE_SIZE = 50;

export default function ActionLogPage() {
  const [logs, setLogs]       = useState<ActionLog[]>([]);
  const [total, setTotal]     = useState(0);
  const [page, setPage]       = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError]     = useState<string | null>(null);

  useEffect(() => { loadLogs(page); }, [page]);

  async function loadLogs(p: number) {
    setLoading(true); setError(null);
    try {
      const res = await get<PagedResult<ActionLog>>(`/actionlogs?page=${p}&pageSize=${PAGE_SIZE}`);
      setLogs(res.items); setTotal(res.totalCount);
    } catch (e) { setError(e instanceof Error ? e.message : 'Ошибка загрузки'); }
    finally { setLoading(false); }
  }

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  return (
    <div className="page">
      <div className="page-header">
        <div className="page-title">
          <h2>Журнал действий</h2>
          <span className="page-subtitle">{total} записей · обновляется автоматически</span>
        </div>
        <button className="btn btn-secondary" onClick={() => loadLogs(page)}>
          ↻ Обновить
        </button>
      </div>

      {error && <ErrorMessage message={error} />}
      {loading ? <LoadingSpinner /> : <ActionLogTable logs={logs} />}

      {!loading && total > PAGE_SIZE && (
        <div className="pagination">
          <button className="btn-page" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page <= 1}>
            ← Назад
          </button>
          <span className="page-info">Страница {page} из {totalPages} · {total} записей</span>
          <button className="btn-page" onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page >= totalPages}>
            Вперёд →
          </button>
        </div>
      )}
    </div>
  );
}
