/* eslint-disable */
import { FuseNavigationItem } from '@fuse/components/navigation';

/**
 * @param isAdmin Booleano que indica se o usuário é administrador.
 */
export const generateNavigation = (isAdmin: boolean): FuseNavigationItem[] => {
    const navigation: FuseNavigationItem[] = [
        {
            id   : 'home',
            title: 'Início',
            type : 'basic',
            icon : 'heroicons_outline:home',
            link : '/home'
        }
    ];

    if ( isAdmin )
    {
        navigation.push({
            id   : 'usuarios',
            title: 'Usuários',
            type : 'basic',
            icon : 'heroicons_outline:user-group',
            link : '/clientes'
        });
    }

    return navigation;
};

// Demais navegações (se não precisarem de lógica, podem continuar estáticas)
// Se elas também dependem do menu principal, elas serão populadas no arquivo api.ts
export const compactNavigation: FuseNavigationItem[] = [];
export const futuristicNavigation: FuseNavigationItem[] = [];
export const horizontalNavigation: FuseNavigationItem[] = [];