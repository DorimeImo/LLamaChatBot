import React, { useContext } from 'react';
import useAxios from '../hooks/useAxios';
import { AuthContext } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';

function LogoutButton() {
    const { clearAuth } = useContext(AuthContext);
    const axiosInstance = useAxios();
    const navigate = useNavigate();

    const handleLogout = async () => {
        try {
            await axiosInstance.post(
                '/api/Auth/logout',
                { userId: authState.userId },
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