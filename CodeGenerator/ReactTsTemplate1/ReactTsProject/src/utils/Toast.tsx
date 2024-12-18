import toast, { ToastOptions } from 'react-hot-toast';

const defaultConfig: ToastOptions = {
  duration: 3000,  // Auto close after 3 seconds
  position: 'top-right',
  style: {
    padding: '16px',
    borderRadius: '8px',
    background: '#333',
    color: '#fff',
    minWidth: '250px',
  },
};

export const Toast = {
  success: (message: string, options?: ToastOptions) =>
    toast.success(message, {
      ...defaultConfig,
      ...options,
      style: {
        ...defaultConfig.style,
        background: '#1a1a1a',
        border: '1px solid #00ff9d',
        boxShadow: '0 4px 12px rgba(0, 255, 157, 0.2)',
      },
      icon: '✅',
    }),

  error: (message: string, options?: ToastOptions) =>
    toast.error(message, {
      ...defaultConfig,
      ...options,
      style: {
        ...defaultConfig.style,
        background: '#1a1a1a',
        border: '1px solid #ff4d4d',
        boxShadow: '0 4px 12px rgba(255, 77, 77, 0.2)',
      },
      icon: '❌',
    }),

  warning: (message: string, options?: ToastOptions) =>
    toast(message, {
      ...defaultConfig,
      ...options,
      style: {
        ...defaultConfig.style,
        background: '#1a1a1a',
        border: '1px solid #ffb84d',
        boxShadow: '0 4px 12px rgba(255, 184, 77, 0.2)',
      },
      icon: '⚠️',
    }),

  info: (message: string, options?: ToastOptions) =>
    toast(message, {
      ...defaultConfig,
      ...options,
      style: {
        ...defaultConfig.style,
        background: '#1a1a1a',
        border: '1px solid #4d94ff',
        boxShadow: '0 4px 12px rgba(77, 148, 255, 0.2)',
      },
      icon: 'ℹ️',
    }),

  dismiss: () => toast.dismiss(),
};