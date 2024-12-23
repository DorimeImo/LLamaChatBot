import React, { useContext } from 'react';
import useAxios from '../api/useAxios';
import { AuthContext } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';

function LogoutButton() {
    const { clearAuth } = useContext(AuthContext);
    const navigate = useNavigate();
    const axiosInstance = useAxios();

    const handleLogout = async () => {
        try {
            await axiosInstance.get(
                '/Auth/logout',
                { withCredentials: true }
            );
            clearAuth();
            navigate('/login');
        } catch (err) {
            console.error('Logout failed:', err);
            clearAuth();
            navigate('/login');
        }
    };

    return <button onClick={handleLogout}>Logout</button>;
}

export default LogoutButton;