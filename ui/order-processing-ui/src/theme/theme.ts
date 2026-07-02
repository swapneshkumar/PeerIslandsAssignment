import { alpha, createTheme } from '@mui/material/styles';

export const appTheme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#2563eb',
      dark: '#1d4ed8',
      light: '#93c5fd'
    },
    secondary: {
      main: '#0f766e',
      dark: '#115e59',
      light: '#5eead4'
    },
    warning: {
      main: '#b45309'
    },
    error: {
      main: '#b91c1c'
    },
    success: {
      main: '#15803d'
    },
    background: {
      default: '#eef2f7',
      paper: '#ffffff'
    },
    text: {
      primary: '#111827',
      secondary: '#4b5563'
    },
    divider: '#d7deea'
  },
  typography: {
    fontFamily: 'Inter, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
    h1: {
      fontSize: '2rem',
      fontWeight: 760,
      letterSpacing: 0
    },
    h2: {
      fontSize: '1.35rem',
      fontWeight: 740,
      letterSpacing: 0
    },
    h3: {
      fontSize: '1.05rem',
      fontWeight: 720,
      letterSpacing: 0
    },
    button: {
      textTransform: 'none',
      fontWeight: 700,
      letterSpacing: 0
    }
  },
  shape: {
    borderRadius: 8
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          minHeight: 38,
          boxShadow: 'none',
          borderRadius: 7
        }
      }
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          borderRadius: 8
        }
      }
    },
    MuiChip: {
      styleOverrides: {
        root: {
          fontWeight: 700
        }
      }
    },
    MuiTableCell: {
      styleOverrides: {
        head: ({ theme }) => ({
          backgroundColor: alpha(theme.palette.primary.main, 0.06),
          color: theme.palette.text.secondary,
          fontSize: 12,
          fontWeight: 800,
          textTransform: 'uppercase',
          letterSpacing: 0
        })
      }
    }
  }
});
