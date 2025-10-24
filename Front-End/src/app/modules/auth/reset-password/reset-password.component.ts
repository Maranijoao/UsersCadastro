import { NgIf } from '@angular/common';
import { Component, OnInit, viewChild, ViewChild, ViewEncapsulation } from '@angular/core';
import { FormsModule, NgForm, ReactiveFormsModule, UntypedFormBuilder, UntypedFormGroup, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, Router, RouterLink } from '@angular/router'; // Importar ActivatedRoute e Router
import { fuseAnimations } from '@fuse/animations';
import { FuseAlertComponent, FuseAlertType } from '@fuse/components/alert';
import { FuseValidators } from '@fuse/validators';
import { AuthService } from 'app/core/auth/auth.service'; // Importar o AuthService
import { finalize } from 'rxjs';

@Component({
    selector: 'reset-password-modern-reversed',
    templateUrl: './reset-password.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: fuseAnimations,
    standalone: true,
    imports: [NgIf, FuseAlertComponent, FormsModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, RouterLink],
})
export class ResetPasswordModernReversedComponent implements OnInit {
    @ViewChild('resetPasswordNgForm') resetPasswordNgForm: NgForm;

    alert: { type: FuseAlertType; message: string } = {
        type: 'success',
        message: '',
    };
    resetPasswordForm: UntypedFormGroup;
    showAlert: boolean = false;
    private token: string;

    constructor(
        private _authService: AuthService,
        private _formBuilder: UntypedFormBuilder,
        private _route: ActivatedRoute,
        private _router: Router
    ) {
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Lifecycle hooks
    // -----------------------------------------------------------------------------------------------------

    /**
     * On init
     */
    ngOnInit(): void {
        this.resetPasswordForm = this._formBuilder.group({
            password: ['', Validators.required],
            passwordConfirm: ['', Validators.required],
        },
            {
                validators: FuseValidators.mustMatch('password', 'passwordConfirm'),
            },
        );
        // Ler o token do parâmetro da URL
        this._route.queryParams.subscribe(params => {
            this.token = params['token'];

            if (!this.token) {
                // Se não houver token, redireciona para o login ou mostra um erro
                this._router.navigate(['/sign-in']);
            }
        });

        this.resetPasswordForm = this._formBuilder.group({
            password: ['', Validators.required],
            passwordConfirm: ['', Validators.required],
        },
            {
                validators: FuseValidators.mustMatch('password', 'passwordConfirm'),
            },
        );
    }

    resetPassword(): void {
        if (this.resetPasswordForm.invalid) {
            return;
        }

        this.resetPasswordForm.disable();
        this.showAlert = false;

        const newPassword = this.resetPasswordForm.get('password')?.value;

        this._authService.resetPassword(this.token, newPassword)
            .pipe(
                finalize(() => {
                    this.resetPasswordForm.enable();
                })
            )
            .subscribe(
                (response) => {
                    this.alert = {
                        type: 'success',
                        message: 'Sua senha foi redefinida com sucesso. Você já pode fazer login.',
                    };
                    this.showAlert = true;

                    setTimeout(() => {
                        this._router.navigate(['/sign-in']);
                    }, 3000);
                },
                (error) => {
                    // Lógica melhorada para extrair a mensagem de erro do backend
                    let errorMessage = 'Ocorreu um erro. O seu link pode ser inválido ou ter expirado.';
                    if (error && error.error && error.error.message) {
                        errorMessage = error.error.message;
                    }

                    this.alert = {
                        type: 'error',
                        message: errorMessage,
                    };
                    this.showAlert = true;
                }
            );
    }
}
