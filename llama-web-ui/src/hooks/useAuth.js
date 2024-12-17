import { useState, useEffect, useContext } from 'react';
import useAxios from './useAxios';
import { AuthContext } from '../context/AuthContext';
import useAxios from '../api/useAxios';

function useAuth() {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const { authState, setAuth, clearAuth } = useContext(AuthContext);
    const axiosInstance = useAxios();

    useEffect(() => {
        const verifyAccessToken = async () => {
            if (authState.accessToken) {
                try {
                    await axiosInstance.get('/Auth/verifyToken');
                    setIsAuthenticated(true);
                } catch {
                    setIsAuthenticated(false);
                }
            }
            else
            {
                setIsAuthenticated(false);
            }
        };

        verifyAccessToken();
    }, [authState.accessToken, axiosInstance]);

    return { isAuthenticated, setIsAuthenticated };
}

export default useAuth;