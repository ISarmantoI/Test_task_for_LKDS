export interface ActionLog {
  id: number;
  timestamp: string;
  action: string;
  entityType: string | null;
  entityId: number | null;
  details: string | null;
}

interface ActionLogTableProps {
  logs: ActionLog[];
}

function formatTimestamp(ts: string): string {
  const date = new Date(ts);
  if (isNaN(date.getTime())) return ts;
  return date.toLocaleString('ru-RU', {
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
  });
}

/** Возвращает CSS-класс бейджа в зависимости от типа действия. */
function getBadgeClass(action: string): string {
  const a = action.toLowerCase();
  if (a === 'create') return 'badge badge-create';
  if (a === 'update') return 'badge badge-update';
  if (a === 'delete') return 'badge badge-delete';
  if (a === 'seeddata') return 'badge badge-seed';
  return 'badge badge-default';
}

export default function ActionLogTable({ logs }: ActionLogTableProps) {
  if (logs.length === 0) {
    return <p className="action-log-empty">Записей не найдено</p>;
  }

  return (
    <div className="action-log-table-wrapper">
      <div className="table-wrapper">
        <table>
          <thead>
            <tr>
              <th>Время</th>
              <th>Действие</th>
              <th>Тип</th>
              <th>ID</th>
              <th>Детали</th>
            </tr>
          </thead>
          <tbody>
            {logs.map((log) => (
              <tr key={log.id}>
                <td className="col-timestamp">{formatTimestamp(log.timestamp)}</td>
                <td className="col-action">
                  <span className={getBadgeClass(log.action)}>{log.action}</span>
                </td>
                <td>{log.entityType ?? '—'}</td>
                <td>{log.entityId != null ? log.entityId : '—'}</td>
                <td className="col-details">{log.details ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
