import React, { useState, useContext } from 'react';
import useAxios from '../api/useAxios';
import { AuthContext } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import useAuth from '../hooks/useAuth';

function RegisterPage() {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [email, setEmail] = useState('');
    const [message, setMessage] = useState('');
    
    const { setAuth } = useContext(AuthContext);
    const axiosInstance = useAxios();
    const { setIsAuthenticated } = useAuth();

    const navigate = useNavigate();

    const handleRegister = async () => {
        try {
            console.log({ username, password, email });
            const response = axiosInstance.post('http://localhost:7224/api/Auth/register', { username, password, email }, { withCredentials: true });
            const { accessToken, userId } = response.data;

            setAuth(accessToken, userId);
            setIsAuthenticated(true);

            setMessage('Registration successful. Redirecting...');
            navigate('/chat');
        } catch (err) {
            setMessage(err.response?.data?.Message || 'Registration failed');
        }
    };

    return (
        <div>
            <h2>Register</h2>
            {message && <p>{message}</p>}
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
            <input
                type="email"
                placeholder="Email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
            />
            <button onClick={handleRegister}>Register</button>
        </div>
    );
}

export default RegisterPage;