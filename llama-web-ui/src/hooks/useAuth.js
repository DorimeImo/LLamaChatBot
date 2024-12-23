import { useState, useEffect, useContext } from 'react';
import { AuthContext } from '../context/AuthContext';
import useAxios from '../api/useAxios';

function useAuth() {
    const { authState, setAuth, clearAuth, isAuthenticated, setLoading, loading } = useContext(AuthContext);
    const axiosInstance = useAxios();

    const [isVerifying, setIsVerifying] = useState(false);

    const refreshOrVerifyToken = async () => {
        if (isVerifying) return; 
        setIsVerifying(true);

        try {
            if (!authState.accessToken) {
                console.log('No access token. Attempting refresh...');
                const refreshResponse = await axiosInstance.get('/Auth/refresh', { withCredentials: true });
                const { accessToken } = refreshResponse.data;
                setAuth(accessToken); 
                console.log('Token refreshed successfully.');
            } else {
                console.log('Verifying access token...');
                await axiosInstance.get('/Auth/verifyToken');
                console.log('Access token is valid.');
            }
        } catch (error) {
            console.error('Token refresh/verification failed:', error);
            clearAuth(); 
        } finally {
            setLoading(false); 
            setIsVerifying(false);
        }
    };

    return {
        authState,
        isAuthenticated,
        loading,
        refreshOrVerifyToken, 
    };
}

export default useAuth;