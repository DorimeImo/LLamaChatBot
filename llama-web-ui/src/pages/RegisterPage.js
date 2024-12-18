import React, { useState } from 'react';
import useAxios from '../hooks/useAxios';
import { AuthContext } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';

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
            const response = axiosInstance.post('/api/Auth/register', { username, password, email }, { withCredentials: true });
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