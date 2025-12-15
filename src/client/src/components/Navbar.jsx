import { NavLink } from 'react-router-dom';
import './Navbar.css';

export default function Navbar() {
    return (
        <nav className="navbar">
            <div className="navbar-brand">Beymen Tech</div>
            <div className="navbar-links">
                <NavLink to="/" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
                    Test Client
                </NavLink>


            </div>
        </nav>
    );
}


/*

    <NavLink to="/backend-concepts" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
                    Backend Simulation
                </NavLink>
                <NavLink to="/architecture" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
                    Architecture Demo
                </NavLink>
*/