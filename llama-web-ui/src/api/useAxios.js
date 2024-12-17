import { useEffect } from 'react';
import { useContext } from 'react';
import axiosInstance from '../api/axiosInstance';
import { AuthContext } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import { setupInterceptors } from '../api/setupInterceptors';

const useAxios = () => {
    const authContext = useContext(AuthContext);
    const navigate = useNavigate();

    useEffect(() => {
        setupInterceptors(axiosInstance, authContext, navigate);
        // Empty dependency array ensures interceptors are set up only once
    }, [authContext, navigate]);

    return axiosInstance;
};

export default useAxios;