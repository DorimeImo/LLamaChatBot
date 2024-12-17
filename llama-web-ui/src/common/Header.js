import React, { useContext } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AuthContext } from '../context/AuthContext';
import useAuth from '../hooks/useAuth';

function Header() {
    const { isAuthenticated } = useAuth();
    const { authState, clearAuth } = useContext(AuthContext);
    const navigate = useNavigate();

    const handleLogout = () => {
        clearAuth();
        navigate('/login');
    };

    return (
        <header style={{ padding: '10px', background: '#f5f5f5', borderBottom: '1px solid #ccc' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                {/* Logo */}
                {/* <div>
                    <img
                        src="/path/to/logo.png" // Replace with your logo path
                        alt="Logo"
                        style={{ height: '40px' }}
                    />
                </div> */}

                {/* Links */}
                <div>
                    {!isAuthenticated  ? (
                        <>
                            <Link to="/login" style={{ marginRight: '10px' }}>
                                Login
                            </Link>
                            <Link to="/register">Register</Link>
                        </>
                    ) : (
                        <button onClick={handleLogout} style={{ border: 'none', background: 'transparent' }}>
                            Logout
                        </button>
                    )}
                </div>
            </div>
        </header>
    );
}

export default Header;