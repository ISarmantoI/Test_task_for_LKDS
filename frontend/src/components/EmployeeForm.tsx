import { useState, useEffect, useRef } from 'react';

interface Organization { id: number; name: string; }

interface Employee {
  id: number; organizationId: number;
  firstName: string; lastName: string; middleName: string | null;
  position: string | null; department: string | null;
  phone: string | null; email: string | null;
  birthDate: string | null; hireDate: string | null;
  hasPhoto: boolean; createdAt: string;
}

interface EmployeeFormData {
  organizationId: string; firstName: string; lastName: string;
  middleName: string; position: string; department: string;
  phone: string; email: string; birthDate: string; hireDate: string;
  photo: File | null;
}

interface EmployeeFormProps {
  employee: Employee | null;
  organizations: Organization[];
  onSubmit: (data: EmployeeFormData) => Promise<void>;
  onCancel: () => void;
}

const emptyForm: EmployeeFormData = {
  organizationId: '', firstName: '', lastName: '', middleName: '',
  position: '', department: '', phone: '', email: '',
  birthDate: '', hireDate: '', photo: null,
};

function toDateInput(d: string | null): string {
  return d ? d.substring(0, 10) : '';
}

export default function EmployeeForm({ employee, organizations, onSubmit, onCancel }: EmployeeFormProps) {
  const [formData, setFormData] = useState<EmployeeFormData>(emptyForm);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [photoPreview, setPhotoPreview] = useState<string | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (employee) {
      setFormData({
        organizationId: String(employee.organizationId),
        firstName: employee.firstName, lastName: employee.lastName,
        middleName: employee.middleName ?? '', position: employee.position ?? '',
        department: employee.department ?? '', phone: employee.phone ?? '',
        email: employee.email ?? '', birthDate: toDateInput(employee.birthDate),
        hireDate: toDateInput(employee.hireDate), photo: null,
      });
      setPhotoPreview(employee.hasPhoto ? `/api/employees/${employee.id}/photo` : null);
    } else {
      setFormData(emptyForm);
      setPhotoPreview(null);
    }
    setError(null);
    if (fileRef.current) fileRef.current.value = '';
  }, [employee]);

  function handleChange(e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) {
    setFormData(prev => ({ ...prev, [e.target.name]: e.target.value }));
  }

  function handleFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0] ?? null;
    setFormData(prev => ({ ...prev, photo: file }));
    if (file) {
      setPhotoPreview(URL.createObjectURL(file));
    } else {
      setPhotoPreview(employee?.hasPhoto ? `/api/employees/${employee.id}/photo` : null);
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!formData.firstName.trim()) { setError('Имя обязательно'); return; }
    if (!formData.lastName.trim())  { setError('Фамилия обязательна'); return; }
    if (!formData.organizationId)   { setError('Выберите организацию'); return; }
    setSubmitting(true); setError(null);
    try { await onSubmit(formData); }
    catch (err) { setError(err instanceof Error ? err.message : 'Ошибка при сохранении'); }
    finally { setSubmitting(false); }
  }

  return (
    <div className="form-overlay" role="dialog" aria-modal="true">
      <div className="form-box wide">
        <h3 className="form-title">
          {employee ? '✏️ Редактировать сотрудника' : '➕ Новый сотрудник'}
        </h3>

        {error && <div className="form-error">⚠ {error}</div>}

        <form onSubmit={handleSubmit} noValidate>
          {/* Организация */}
          <div className="form-field">
            <label htmlFor="ef-org">Организация *</label>
            <select id="ef-org" name="organizationId" value={formData.organizationId} onChange={handleChange} required>
              <option value="">— Выберите организацию —</option>
              {organizations.map(o => <option key={o.id} value={String(o.id)}>{o.name}</option>)}
            </select>
          </div>

          {/* ФИО */}
          <div className="form-row">
            <div className="form-field">
              <label htmlFor="ef-last">Фамилия *</label>
              <input id="ef-last" name="lastName" type="text" value={formData.lastName} onChange={handleChange} placeholder="Иванов" required />
            </div>
            <div className="form-field">
              <label htmlFor="ef-first">Имя *</label>
              <input id="ef-first" name="firstName" type="text" value={formData.firstName} onChange={handleChange} placeholder="Иван" required />
            </div>
            <div className="form-field">
              <label htmlFor="ef-mid">Отчество</label>
              <input id="ef-mid" name="middleName" type="text" value={formData.middleName} onChange={handleChange} placeholder="Иванович" />
            </div>
          </div>

          {/* Должность и отдел */}
          <div className="form-row">
            <div className="form-field">
              <label htmlFor="ef-pos">Должность</label>
              <input id="ef-pos" name="position" type="text" value={formData.position} onChange={handleChange} placeholder="Менеджер" />
            </div>
            <div className="form-field">
              <label htmlFor="ef-dep">Отдел</label>
              <input id="ef-dep" name="department" type="text" value={formData.department} onChange={handleChange} placeholder="Бухгалтерия" />
            </div>
          </div>

          {/* Контакты */}
          <div className="form-row">
            <div className="form-field">
              <label htmlFor="ef-phone">Телефон</label>
              <input id="ef-phone" name="phone" type="tel" value={formData.phone} onChange={handleChange} placeholder="+7 (999) 000-00-00" />
            </div>
            <div className="form-field">
              <label htmlFor="ef-email">Email</label>
              <input id="ef-email" name="email" type="email" value={formData.email} onChange={handleChange} placeholder="ivan@mail.ru" />
            </div>
          </div>

          {/* Даты */}
          <div className="form-row">
            <div className="form-field">
              <label htmlFor="ef-birth">Дата рождения</label>
              <input id="ef-birth" name="birthDate" type="date" value={formData.birthDate} onChange={handleChange} />
            </div>
            <div className="form-field">
              <label htmlFor="ef-hire">Дата приёма</label>
              <input id="ef-hire" name="hireDate" type="date" value={formData.hireDate} onChange={handleChange} />
            </div>
          </div>

          {/* Фото */}
          <div className="form-field">
            <label htmlFor="ef-photo">Фото</label>
            <input id="ef-photo" name="photo" type="file" accept="image/*" ref={fileRef} onChange={handleFile} />
            {photoPreview && (
              <div className="emp-photo-preview">
                <img src={photoPreview} alt="Предпросмотр" className="emp-photo-full" />
              </div>
            )}
          </div>

          <div className="form-actions">
            <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={submitting}>Отмена</button>
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              {submitting ? 'Сохранение...' : 'Сохранить'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
