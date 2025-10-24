import { Directive, ElementRef, HostListener, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Directive({
    selector: '[appDateMask]',
    standalone: true,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => DateMaskDirective),
            multi: true,
        },
    ],
})
export class DateMaskDirective implements ControlValueAccessor {
    private onChange: (value: any) => void = () => {};
    private onTouched: () => void = () => {};

    constructor(private el: ElementRef) {}

    @HostListener('input', ['$event.target.value'])
    onInput(value: string): void {
        const formattedValue = this.formatDigits(value.replace(/\D/g, ''));
        this.updateValue(formattedValue);
    }

    @HostListener('blur', ['$event.target.value'])
    onBlur(value: string): void {
        this.onTouched();
        // Reformata na saída para garantir
        const formattedValue = this.formatDigits(value.replace(/\D/g, ''));
        this.updateValue(formattedValue);
    }

   
    private formatDigits(digits: string): string {
        if (!digits) {
            return '';
        }
      
        digits = digits.substring(0, 8);

        if (digits.length <= 2) {
            return digits;
        } else if (digits.length <= 4) {
            return `${digits.substring(0, 2)}/${digits.substring(2)}`;
        } else {
            return `${digits.substring(0, 2)}/${digits.substring(2, 4)}/${digits.substring(4)}`;
        }
    }

    // Função para tentar converter AAAA-MM-DD para DD/MM/AAAA
    private tryFormatIsoToBr(value: string): string {
         if (/^\d{4}-\d{2}-\d{2}/.test(value)) {
             try {
                 const parts = value.substring(0, 10).split('-'); // AAAA, MM, DD
                 return `${parts[2]}/${parts[1]}/${parts[0]}`; // Retorna DD/MM/AAAA
             } catch {
                
             }
        }
        // Se não for AAAA-MM-DD, retorna o valor original para ser tratado
        return value;
    }


    private updateValue(value: string): void {
        this.el.nativeElement.value = value;
        this.onChange(value); // Envia sempre o valor formatado (DD/MM/AAAA)
    }

    // --- ControlValueAccessor Methods ---
    writeValue(value: any): void {
        let displayValue = '';
        if (typeof value === 'string') {
            // 1. Tenta converter AAAA-MM-DD para DD/MM/AAAA
            const formattedFromIso = this.tryFormatIsoToBr(value);
            // 2. Aplica a formatação final baseada nos dígitos
            displayValue = this.formatDigits(formattedFromIso.replace(/\D/g, ''));
        }
        // Define o valor inicial formatado no input
        this.el.nativeElement.value = displayValue;
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    setDisabledState?(isDisabled: boolean): void {
        this.el.nativeElement.disabled = isDisabled;
    }
}