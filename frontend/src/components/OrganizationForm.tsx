import { useState, useEffect } from 'react';

interface Organization {
  id: number;
  name: string;
  legalAddress: string | null;
  phone: string | null;
  email: string | null;
  inn: string | null;
  createdAt: string;
}

interface OrganizationFormData {
  name: string;
  legalAddress: string;
  phone: string;
  email: string;
  inn: string;
}

interface OrganizationFormProps {
  organization: Organization | null;
  onSubmit: (data: OrganizationFormData) => Promise<void>;
  onCancel: () => void;
}

const emptyForm: OrganizationFormData = { name: '', legalAddress: '', phone: '', email: '', inn: '' };

export default function OrganizationForm({ organization, onSubmit, onCancel }: OrganizationFormProps) {
  const [formData, setFormData] = useState<OrganizationFormData>(emptyForm);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (organization) {
      setFormData({
        name: organization.name,
        legalAddress: organization.legalAddress ?? '',
        phone: organization.phone ?? '',
        email: organization.email ?? '',
        inn: organization.inn ?? '',
      });
    } else {
      setFormData(emptyForm);
    }
    setError(null);
  }, [organization]);

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!formData.name.trim()) { setError('Название обязательно для заполнения'); return; }
    setSubmitting(true);
    setError(null);
    try {
      await onSubmit(formData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ошибка при сохранении');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="form-overlay" role="dialog" aria-modal="true">
      <div className="form-box">
        <h3 className="form-title">
          {organization ? '✏️ Редактировать организацию' : '➕ Новая организация'}
        </h3>

        {error && <div className="form-error">⚠ {error}</div>}

        <form onSubmit={handleSubmit} noValidate>
          <div className="form-field">
            <label htmlFor="org-name">Название *</label>
            <input
              id="org-name" name="name" type="text"
              value={formData.name} onChange={handleChange}
              placeholder="ООО Ромашка" autoFocus required
            />
          </div>

          <div className="form-field">
            <label htmlFor="org-addr">Юридический адрес</label>
            <input
              id="org-addr" name="legalAddress" type="text"
              value={formData.legalAddress} onChange={handleChange}
              placeholder="г. Москва, ул. Ленина, д. 1"
            />
          </div>

          <div className="form-row">
            <div className="form-field">
              <label htmlFor="org-phone">Телефон</label>
              <input
                id="org-phone" name="phone" type="tel"
                value={formData.phone} onChange={handleChange}
                placeholder="+7 (999) 000-00-00"
              />
            </div>
            <div className="form-field">
              <label htmlFor="org-inn">ИНН</label>
              <input
                id="org-inn" name="inn" type="text"
                value={formData.inn} onChange={handleChange}
                placeholder="1234567890"
              />
            </div>
          </div>

          <div className="form-field">
            <label htmlFor="org-email">Email</label>
            <input
              id="org-email" name="email" type="email"
              value={formData.email} onChange={handleChange}
              placeholder="info@company.ru"
            />
          </div>

          <div className="form-actions">
            <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={submitting}>
              Отмена
            </button>
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              {submitting ? 'Сохранение...' : 'Сохранить'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
