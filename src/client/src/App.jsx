import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Navbar from './components/Navbar';
import TestClientPage from './pages/TestClientPage';
import BackendDashboardPage from './pages/BackendDashboardPage';
import ArchitectureDashboardPage from './pages/ArchitectureDashboardPage';
import './App.css';

function App() {
  return (
    <Router>
      <div className="app-layout">
        <Navbar />
        <main className="content">
          <Routes>
            <Route path="/" element={<TestClientPage />} />
            <Route path="/backend-concepts" element={<BackendDashboardPage />} />
            <Route path="/architecture" element={<ArchitectureDashboardPage />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </main>
      </div>
    </Router>
  );
}

export default App;
