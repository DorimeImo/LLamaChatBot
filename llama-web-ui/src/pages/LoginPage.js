import React, { useState, useContext } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';
import { AuthContext } from '../context/AuthContext';
import useAuth from '../hooks/useAuth';

function LoginPage() {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');

    const { setAuth } = useContext(AuthContext); 
    const { setIsAuthenticated } = useAuth();

    const navigate = useNavigate();

    const handleLogin = async () => {
        try {
            const response = await axios.post('http://localhost:7224/api/Auth/login', { username, password }, 
                { withCredentials: true });

            console.log(`Response is received`);    
            const { accessToken, userId } = response.data;
            console.log(`accessToken: ${accessToken}, userId: ${userId}`);

            setAuth(accessToken, userId);
            setIsAuthenticated(true);

            console.log('Login successful, navigating to /chat');
            navigate('/chat');
        } catch (err) {
            setError(err.response?.data?.Message || 'Login failed');
            
            if (err.response?.status === 404) {
                navigate('/register');
            }
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