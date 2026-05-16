interface Organization { id: number; name: string; }

interface Employee {
  id: number; organizationId: number;
  firstName: string; lastName: string; middleName: string | null;
  position: string | null; department: string | null;
  phone: string | null; email: string | null;
  birthDate: string | null; hireDate: string | null;
  hasPhoto: boolean; createdAt: string;
}

interface EmployeesTableProps {
  employees: Employee[];
  organizations: Organization[];
  onEdit: (emp: Employee) => void;
  onDelete: (emp: Employee) => void;
}

function formatDate(d: string | null): string {
  if (!d) return '—';
  return new Date(d).toLocaleDateString('ru-RU', { year: 'numeric', month: '2-digit', day: '2-digit' });
}

export default function EmployeesTable({ employees, organizations, onEdit, onDelete }: EmployeesTableProps) {
  const orgMap = new Map(organizations.map(o => [o.id, o.name]));

  if (employees.length === 0) {
    return (
      <div className="table-wrapper">
        <p className="table-empty">Сотрудники не найдены. Измените фильтры или добавьте сотрудника.</p>
      </div>
    );
  }

  return (
    <div className="table-wrapper">
      <table>
        <thead>
          <tr>
            <th>Фото</th>
            <th>ФИО</th>
            <th>Должность / Отдел</th>
            <th>Контакты</th>
            <th>Даты</th>
            <th>Организация</th>
            <th>Действия</th>
          </tr>
        </thead>
        <tbody>
          {employees.map(emp => (
            <tr key={emp.id}>
              {/* Фото */}
              <td className="emp-photo-cell">
                {emp.hasPhoto
                  ? <img src={`/api/employees/${emp.id}/photo`} alt="" className="emp-thumbnail" />
                  : <div className="emp-photo-placeholder">👤</div>
                }
              </td>

              {/* ФИО — объединяем в одну ячейку для компактности */}
              <td>
                <div style={{ fontWeight: 600, color: 'var(--text-heading)', lineHeight: 1.3 }}>
                  {emp.lastName} {emp.firstName}
                </div>
                {emp.middleName && (
                  <div style={{ fontSize: '0.78rem', color: 'var(--text-muted)' }}>{emp.middleName}</div>
                )}
              </td>

              {/* Должность + отдел */}
              <td>
                {emp.position && (
                  <div style={{ fontWeight: 500, fontSize: '0.85rem' }}>{emp.position}</div>
                )}
                {emp.department && (
                  <div style={{ fontSize: '0.78rem', color: 'var(--text-muted)' }}>{emp.department}</div>
                )}
                {!emp.position && !emp.department && <span style={{ color: 'var(--text-muted)' }}>—</span>}
              </td>

              {/* Контакты */}
              <td>
                {emp.phone && <div style={{ fontSize: '0.82rem' }}>{emp.phone}</div>}
                {emp.email && <div style={{ fontSize: '0.78rem', color: 'var(--text-muted)' }}>{emp.email}</div>}
                {!emp.phone && !emp.email && <span style={{ color: 'var(--text-muted)' }}>—</span>}
              </td>

              {/* Даты */}
              <td>
                <div style={{ fontSize: '0.78rem', color: 'var(--text-secondary)' }}>
                  <div>Рожд: {formatDate(emp.birthDate)}</div>
                  <div>Приём: {formatDate(emp.hireDate)}</div>
                </div>
              </td>

              {/* Организация */}
              <td style={{ fontSize: '0.82rem', color: 'var(--text-secondary)' }}>
                {orgMap.get(emp.organizationId) ?? '—'}
              </td>

              {/* Действия */}
              <td>
                <div className="table-actions">
                  <button className="btn btn-edit" onClick={() => onEdit(emp)}>Изменить</button>
                  <button className="btn btn-danger" onClick={() => onDelete(emp)}>Удалить</button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
