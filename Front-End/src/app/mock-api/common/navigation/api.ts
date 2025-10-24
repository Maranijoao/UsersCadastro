import { Injectable } from '@angular/core';
import { FuseNavigationItem } from '@fuse/components/navigation';
import { FuseMockApiService } from '@fuse/lib/mock-api';
import { compactNavigation, generateNavigation, futuristicNavigation, horizontalNavigation } from 'app/mock-api/common/navigation/data';
import { clone, cloneDeep, compact } from 'lodash-es';
import { AuthService } from 'app/core/auth/auth.service';
import { UserService } from 'app/core/user/user.service';

@Injectable({providedIn: 'root'})
export class NavigationMockApi
{
    /**
     * Constructor
     */
    constructor(
        private _fuseMockApiService: FuseMockApiService,
    )
    {
        // Register Mock API handlers\
        this.registerHandlers();
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Public methods
    // -----------------------------------------------------------------------------------------------------

    /**
     * Register Mock API handlers
     */
    registerHandlers(): void
    {
        // -----------------------------------------------------------------------------------------------------
        // @ Navigation - GET
        // -----------------------------------------------------------------------------------------------------
        this._fuseMockApiService
            .onGet('api/common/navigation')
            .reply(() =>
            {
                const isAdmin = true;
                const defaultNavigation = generateNavigation(isAdmin);
                const compactNav = cloneDeep(compactNavigation);
                const futuristicNav = cloneDeep(futuristicNavigation);
                const horizontalNav = cloneDeep(horizontalNavigation);

                compactNav.forEach((item) => {
                    const found = defaultNavigation.find(d => d.id === item.id);
                    if (found && found.children) {
                        item.children = cloneDeep(found.children);
                    }
                });

                futuristicNav.forEach((item) => {
                    const found = defaultNavigation.find(d => d.id === item.id);
                    if (found && found.children) {
                        item.children = cloneDeep(found.children);
                    }
                });

                horizontalNav.forEach((item) => {
                    const found = defaultNavigation.find(d => d.id === item.id);
                    if (found && found.children) {
                        item.children = cloneDeep(found.children);
                    }
                });

                return [
                    200,
                    {
                        compact   : (compactNav),
                        default   : cloneDeep(defaultNavigation),
                        futuristic : (compactNav),
                        horizontal: (compactNav)
                    },
                ];
            });
    }
}
