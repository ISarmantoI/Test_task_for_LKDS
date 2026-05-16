import { useState, useEffect, useCallback } from 'react';
import * as api from '../api/apiClient';
import SearchPanel, { SearchFilters, emptyFilters } from '../components/SearchPanel';
import EmployeesTable from '../components/EmployeesTable';
import EmployeeForm from '../components/EmployeeForm';
import ConfirmDialog from '../components/ConfirmDialog';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

interface Organization { id: number; name: string; }

interface Employee {
  id: number; organizationId: number;
  firstName: string; lastName: string; middleName: string | null;
  position: string | null; department: string | null;
  phone: string | null; email: string | null;
  birthDate: string | null; hireDate: string | null;
  hasPhoto: boolean; createdAt: string;
}

interface PagedResult<T> { items: T[]; totalCount: number; page: number; pageSize: number; }

interface EmpFormData {
  organizationId: string; firstName: string; lastName: string; middleName: string;
  position: string; department: string; phone: string; email: string;
  birthDate: string; hireDate: string; photo: File | null;
}

const PAGE_SIZE = 20;

function buildQuery(f: SearchFilters, page: number): string {
  const p = new URLSearchParams();
  if (f.lastName.trim())      p.set('lastName',      f.lastName.trim());
  if (f.firstName.trim())     p.set('firstName',     f.firstName.trim());
  if (f.middleName.trim())    p.set('middleName',    f.middleName.trim());
  if (f.position.trim())      p.set('position',      f.position.trim());
  if (f.department.trim())    p.set('department',    f.department.trim());
  if (f.email.trim())         p.set('email',         f.email.trim());
  if (f.phone.trim())         p.set('phone',         f.phone.trim());
  if (f.organizationId)       p.set('organizationId',f.organizationId);
  p.set('page', String(page));
  p.set('pageSize', String(PAGE_SIZE));
  return p.toString();
}

export default function EmployeesPage() {
  const [orgs, setOrgs]           = useState<Organization[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [total, setTotal]         = useState(0);
  const [page, setPage]           = useState(1);
  const [filters, setFilters]     = useState<SearchFilters>(emptyFilters);
  const [applied, setApplied]     = useState<SearchFilters>(emptyFilters);
  const [loading, setLoading]     = useState(false);
  const [error, setError]         = useState<string | null>(null);
  const [formOpen, setFormOpen]   = useState(false);
  const [editing, setEditing]     = useState<Employee | null>(null);
  const [delTarget, setDelTarget] = useState<Employee | null>(null);

  useEffect(() => {
    api.get<Organization[]>('/organizations').then(setOrgs).catch(() => {});
  }, []);

  const load = useCallback(async (f: SearchFilters, p: number) => {
    setLoading(true); setError(null);
    try {
      const res = await api.get<PagedResult<Employee>>(`/employees?${buildQuery(f, p)}`);
      setEmployees(res.items); setTotal(res.totalCount);
    } catch (e) { setError(e instanceof Error ? e.message : 'Ошибка загрузки'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(applied, page); }, [load, applied, page]);

  function handleSearch() { setApplied(filters); setPage(1); }
  function handleReset()  { setFilters(emptyFilters); setApplied(emptyFilters); setPage(1); }

  async function handleSubmit(data: EmpFormData) {
    const fd = new FormData();
    fd.append('organizationId', data.organizationId);
    fd.append('firstName', data.firstName.trim());
    fd.append('lastName',  data.lastName.trim());
    if (data.middleName.trim()) fd.append('middleName', data.middleName.trim());
    if (data.position.trim())   fd.append('position',   data.position.trim());
    if (data.department.trim()) fd.append('department', data.department.trim());
    if (data.phone.trim())      fd.append('phone',      data.phone.trim());
    if (data.email.trim())      fd.append('email',      data.email.trim());
    if (data.birthDate)         fd.append('birthDate',  data.birthDate);
    if (data.hireDate)          fd.append('hireDate',   data.hireDate);
    if (data.photo)             fd.append('photo',      data.photo);

    if (editing) await api.putForm<Employee>(`/employees/${editing.id}`, fd);
    else         await api.postForm<Employee>('/employees', fd);

    setFormOpen(false); setEditing(null);
    await load(applied, page);
  }

  async function handleDelete() {
    if (!delTarget) return;
    const t = delTarget; setDelTarget(null);
    try { await api.del(`/employees/${t.id}`); await load(applied, page); }
    catch (e) { setError(e instanceof Error ? e.message : 'Ошибка удаления'); }
  }

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  return (
    <div className="page">
      <div className="page-header">
        <div className="page-title">
          <h2>Сотрудники</h2>
          <span className="page-subtitle">{total} записей</span>
        </div>
        <button className="btn btn-primary" onClick={() => { setEditing(null); setFormOpen(true); }}>
          + Добавить
        </button>
      </div>

      <SearchPanel
        filters={filters} organizations={orgs}
        onFiltersChange={setFilters} onSearch={handleSearch} onReset={handleReset}
      />

      {error && <ErrorMessage message={error} />}

      {loading ? <LoadingSpinner /> : (
        <EmployeesTable
          employees={employees} organizations={orgs}
          onEdit={emp => { setEditing(emp); setFormOpen(true); }}
          onDelete={setDelTarget}
        />
      )}

      {/* Пагинация */}
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

      {formOpen && (
        <EmployeeForm
          employee={editing} organizations={orgs}
          onSubmit={handleSubmit}
          onCancel={() => { setFormOpen(false); setEditing(null); }}
        />
      )}

      <ConfirmDialog
        isOpen={delTarget !== null}
        message={delTarget ? `Удалить сотрудника «${delTarget.lastName} ${delTarget.firstName}»?` : ''}
        onConfirm={handleDelete}
        onCancel={() => setDelTarget(null)}
      />
    </div>
  );
}
