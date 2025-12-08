/* eslint-disable */
import { FuseNavigationItem } from '@fuse/components/navigation';

/**
 * @param isAdmin 
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

    navigation.push({
        id   : 'simulacoes',
        title: 'Simulações',
        type : 'basic',
        icon : 'heroicons_outline:calculator',
        link : '/simulations' 
    });

    navigation.push({
        id   : 'emprestimos',
        title: 'Empréstimos',
        type : 'basic',
        icon : 'heroicons_outline:clipboard-document-check', 
        link : '/loans' 
    });

    return navigation;
};

export const compactNavigation: FuseNavigationItem[] = [];
export const futuristicNavigation: FuseNavigationItem[] = [];
export const horizontalNavigation: FuseNavigationItem[] = [];