import { useState, useEffect, useContext } from 'react';
import { AuthContext } from '../context/AuthContext';
import useAxios from '../api/useAxios';

function useAuth() {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [loading, setLoading] = useState(true);
    const {authState} = useContext(AuthContext);
    const axiosInstance = useAxios();

    useEffect(() => {
        console.log('useAuth called.');
    
        const verifyAccessToken = async () => {
            if (!authState.accessToken) {
                console.log('No access token. Setting isAuthenticated to false.');
                
                try {
                    const refreshResponse = await axiosInstance.get(
                        '/Auth/refresh',
                        { withCredentials: true }
                    );
                    const { accessToken } = refreshResponse.data;
            
                    authContext.setAuth(accessToken);
                    setIsAuthenticated(true);
                    console.log('Refresh successful. Setting isAuthenticated to true.');
                } catch (error) {
                    console.error('Refresh failed. Setting isAuthenticated to false.', error);
                    authContext.clearAuth();
                    setIsAuthenticated(false);
                } finally {
                    setLoading(false);
                }
                return;
            }
    
            console.log('Verifying access token...');
            try {
                await axiosInstance.get('/Auth/verifyToken');
                setIsAuthenticated(true);
                console.log('Access token is valid.');
            } catch (error) {
                console.error('Access token verification failed.', error);
                setIsAuthenticated(false);
            } finally {
                setLoading(false);
            }
        };
    
        verifyAccessToken();
    }, [authState.accessToken]);

    return { isAuthenticated, loading, setIsAuthenticated };
}

export default useAuth;