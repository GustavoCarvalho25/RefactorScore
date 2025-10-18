import { ref } from 'vue';

export type Theme = 'light' | 'dark';

const isDark = ref<boolean>(false);
const themeChangeCounter = ref<number>(0);

export function useTheme() {
  const toggleTheme = () => {
    isDark.value = !isDark.value;
    applyTheme();
    themeChangeCounter.value++;
  };

  const setTheme = (theme: Theme) => {
    isDark.value = theme === 'dark';
    applyTheme();
    themeChangeCounter.value++;
  };

  const applyTheme = () => {
    if (isDark.value) {
      document.documentElement.classList.add('dark-mode');
      localStorage.setItem('theme', 'dark');
    } else {
      document.documentElement.classList.remove('dark-mode');
      localStorage.setItem('theme', 'light');
    }
  };

  const initTheme = () => {
    const savedTheme = localStorage.getItem('theme') as Theme | null;
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    
    if (savedTheme) {
      isDark.value = savedTheme === 'dark';
    } else {
      isDark.value = prefersDark;
    }
    
    applyTheme();
  };

  return {
    isDark,
    themeChangeCounter,
    toggleTheme,
    setTheme,
    initTheme,
  };
}

