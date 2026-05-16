interface Organization {
  id: number;
  name: string;
  legalAddress: string | null;
  phone: string | null;
  email: string | null;
  inn: string | null;
  createdAt: string;
}

interface OrganizationsTableProps {
  organizations: Organization[];
  onEdit: (org: Organization) => void;
  onDelete: (org: Organization) => void;
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('ru-RU', {
    year: 'numeric', month: '2-digit', day: '2-digit',
  });
}

export default function OrganizationsTable({ organizations, onEdit, onDelete }: OrganizationsTableProps) {
  if (organizations.length === 0) {
    return (
      <div className="table-wrapper">
        <p className="table-empty">Организации не найдены. Добавьте первую или сгенерируйте тестовые данные.</p>
      </div>
    );
  }

  return (
    <div className="table-wrapper">
      <table>
        <thead>
          <tr>
            <th>#</th>
            <th>Название</th>
            <th>Юр. адрес</th>
            <th>Телефон</th>
            <th>Email</th>
            <th>ИНН</th>
            <th>Создана</th>
            <th>Действия</th>
          </tr>
        </thead>
        <tbody>
          {organizations.map((org) => (
            <tr key={org.id}>
              <td style={{ color: 'var(--text-muted)', fontSize: '0.75rem' }}>{org.id}</td>
              <td style={{ fontWeight: 500, color: 'var(--text-heading)' }}>{org.name}</td>
              <td>{org.legalAddress ?? <span style={{ color: 'var(--text-muted)' }}>—</span>}</td>
              <td>{org.phone ?? <span style={{ color: 'var(--text-muted)' }}>—</span>}</td>
              <td>{org.email ?? <span style={{ color: 'var(--text-muted)' }}>—</span>}</td>
              <td style={{ fontFamily: 'monospace', fontSize: '0.8rem' }}>
                {org.inn ?? <span style={{ color: 'var(--text-muted)' }}>—</span>}
              </td>
              <td style={{ color: 'var(--text-secondary)', fontSize: '0.8rem' }}>{formatDate(org.createdAt)}</td>
              <td>
                <div className="table-actions">
                  <button className="btn btn-edit" onClick={() => onEdit(org)} aria-label={`Редактировать ${org.name}`}>
                    Изменить
                  </button>
                  <button className="btn btn-danger" onClick={() => onDelete(org)} aria-label={`Удалить ${org.name}`}>
                    Удалить
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
