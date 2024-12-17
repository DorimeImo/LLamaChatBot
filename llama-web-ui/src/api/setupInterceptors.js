import axios from 'axios';
import { setupAxiosInterceptors } from './axiosInstance'; // Ensure correct import path

export const setupInterceptors = (axiosInstance, authContext, navigate) => {
    // Request Interceptor
    axiosInstance.interceptors.request.use(
        (config) => {
            if (authContext.authState.accessToken) {
                config.headers['Authorization'] = `Bearer ${authContext.authState.accessToken}`;
            }
            return config;
        },
        (error) => Promise.reject(error)
    );

    // Response Interceptor
    axiosInstance.interceptors.response.use(
        (response) => response,
        async (error) => {
            const originalRequest = error.config;

            if (error.response && error.response.status === 401 && !originalRequest._retry) {
                originalRequest._retry = true;
                try {
                    const refreshResponse = await axios.post(
                        '/api/Auth/refresh',
                        { userId: authContext.authState.userId },
                        { withCredentials: true }
                    );
                    const { accessToken } = refreshResponse.data;

                    authContext.setAuth(accessToken, authContext.authState.userId);

                    originalRequest.headers['Authorization'] = `Bearer ${accessToken}`;
                    return axiosInstance(originalRequest);
                } catch (refreshError) {
                    authContext.clearAuth();
                    navigate('/login');
                    return Promise.reject(refreshError);
                }
            }

            return Promise.reject(error);
        }
    );
};