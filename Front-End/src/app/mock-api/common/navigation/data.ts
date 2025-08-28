/* eslint-disable */
import { FuseNavigationItem } from '@fuse/components/navigation';

export const defaultNavigation: FuseNavigationItem[] = [
    {
        id: 'home',
        title: 'Home',
        type: 'basic',
        icon: 'heroicons_mini:home',
        link: '/home'
    },
    // {
    //     id: 'clientes',
    //     title: 'Clientes',
    //     type: 'basic',
    //     icon: 'heroicons_outline:user-group',
    //     link: '/clientes'
    // },
    {
        id: 'teste',
        title: 'Usuários',
        type: 'basic',
        icon: 'heroicons_outline:user-group',
        link: '/clientes'
    },
];

// Reutilizando o mesmo menu para os outros tipos:
export const futuristicNavigation: FuseNavigationItem[] = [...defaultNavigation];
export const horizontalNavigation: FuseNavigationItem[] = [...defaultNavigation];
export const compactNavigation: FuseNavigationItem[] = [...defaultNavigation]; //
