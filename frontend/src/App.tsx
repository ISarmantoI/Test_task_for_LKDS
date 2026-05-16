import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import NavBar from './components/NavBar';
import OrganizationsPage from './pages/OrganizationsPage';
import EmployeesPage from './pages/EmployeesPage';
import ActionLogPage from './pages/ActionLogPage';

export default function App() {
  return (
    <BrowserRouter>
      <NavBar />
      <main>
        <Routes>
          <Route path="/" element={<Navigate to="/organizations" replace />} />
          <Route path="/organizations" element={<OrganizationsPage />} />
          <Route path="/employees"     element={<EmployeesPage />} />
          <Route path="/actionlog"     element={<ActionLogPage />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}
