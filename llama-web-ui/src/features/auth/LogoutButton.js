import React, { useContext } from 'react';
import axios from 'axios';
import { AuthContext } from './GlobalAuthContext';
import { useNavigate } from 'react-router-dom';

function LogoutButton() {
    const { clearAuth } = useContext(AuthContext);
    const navigate = useNavigate();

    const handleLogout = async () => {
        try {
            await axios.post(
                '/api/Auth/logout',
                { userId: authState.userId }, // Include userId in the request
                { withCredentials: true }
            );
            clearAuth();
            setIsAuthenticated(false);
            navigate('/login');
        } catch (err) {
            console.error('Logout failed:', err);
            clearAuth();
            setIsAuthenticated(false);
            navigate('/login');
        }
    };

    return <button onClick={handleLogout}>Logout</button>;
}

export default LogoutButton;