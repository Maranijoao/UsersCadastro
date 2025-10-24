import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router, UrlTree } from '@angular/router';
import { AuthService } from 'app/core/auth/auth.service';
import { of, switchMap, Observable } from 'rxjs';

export const AuthGuard: CanActivateFn | CanActivateChildFn = (route, state): Observable<boolean | UrlTree> => {
    const router: Router = inject(Router);
    const authService: AuthService = inject(AuthService);

    return authService.check().pipe(
        switchMap((authenticated) => {
            if (!authenticated) {
                const redirectURL = state.url === '/sign-out' ? '' : `redirectURL=${state.url}`;
                const urlTree = router.parseUrl(`sign-in?${redirectURL}`);

                return of(urlTree);
            }

            return of(true);
        }),
    );
};