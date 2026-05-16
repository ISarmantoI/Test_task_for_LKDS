import { useState, useEffect, useCallback } from 'react';
import * as api from '../api/apiClient';
import OrganizationsTable from '../components/OrganizationsTable';
import OrganizationForm from '../components/OrganizationForm';
import ConfirmDialog from '../components/ConfirmDialog';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorMessage from '../components/ErrorMessage';

interface Organization {
  id: number; name: string; legalAddress: string | null;
  phone: string | null; email: string | null; inn: string | null; createdAt: string;
}

interface OrgFormData { name: string; legalAddress: string; phone: string; email: string; inn: string; }

export default function OrganizationsPage() {
  const [orgs, setOrgs] = useState<Organization[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Organization | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Organization | null>(null);

  const load = useCallback(async () => {
    setLoading(true); setError(null);
    try { setOrgs(await api.get<Organization[]>('/organizations')); }
    catch (e) { setError(e instanceof Error ? e.message : 'Ошибка загрузки'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  async function handleSubmit(data: OrgFormData) {
    const body = { name: data.name, legalAddress: data.legalAddress || null, phone: data.phone || null, email: data.email || null, inn: data.inn || null };
    if (editing) await api.put<Organization>(`/organizations/${editing.id}`, body);
    else await api.post<Organization>('/organizations', body);
    setFormOpen(false); setEditing(null);
    await load();
  }

  async function handleDelete() {
    if (!deleteTarget) return;
    const t = deleteTarget; setDeleteTarget(null);
    try { await api.del(`/organizations/${t.id}`); await load(); }
    catch (e) { setError(e instanceof Error ? e.message : 'Ошибка удаления'); }
  }

  return (
    <div className="page">
      <div className="page-header">
        <div className="page-title">
          <h2>Организации</h2>
          <span className="page-subtitle">{orgs.length} записей</span>
        </div>
        <button className="btn btn-primary" onClick={() => { setEditing(null); setFormOpen(true); }}>
          + Добавить
        </button>
      </div>

      {error && <ErrorMessage message={error} />}
      {loading ? <LoadingSpinner /> : (
        <OrganizationsTable
          organizations={orgs}
          onEdit={org => { setEditing(org); setFormOpen(true); }}
          onDelete={setDeleteTarget}
        />
      )}

      {formOpen && (
        <OrganizationForm
          organization={editing}
          onSubmit={handleSubmit}
          onCancel={() => { setFormOpen(false); setEditing(null); }}
        />
      )}

      <ConfirmDialog
        isOpen={deleteTarget !== null}
        message={deleteTarget ? `Удалить организацию «${deleteTarget.name}»? Все сотрудники также будут удалены.` : ''}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  );
}
