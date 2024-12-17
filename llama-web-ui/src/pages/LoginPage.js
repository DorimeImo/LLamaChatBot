import React, { useState } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';
import { AuthContext } from '../context/AuthContext';
import useAuth from '../hooks/useAuth';

function LoginPage() {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [message, setMessage] = useState('');

    const { setAuth } = useContext(AuthContext); 
    const { setIsAuthenticated } = useAuth();

    const navigate = useNavigate();

    const handleLogin = async () => {
        try {
            const response = await axios.post('/api/Auth/login', { username, password }, 
                { withCredentials: true });

            const { accessToken, userId } = response.data;
            setAuth(accessToken, userId);
            setIsAuthenticated(true);

            navigate('/chat');
        } catch (err) {
            setError(err.response?.data?.Message || 'Login failed');
            
            navigate('/register');
        }
    };

    return (
        <div>
            <h2>Login</h2>
            {error && <p style={{ color: 'red' }}>{error}</p>}
            <input
                type="text"
                placeholder="Username"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
            />
            <input
                type="password"
                placeholder="Password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
            />
            <button onClick={handleLogin}>Login</button>
        </div>
    );
}

export default LoginPage;