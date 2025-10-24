import { Component, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from "@angular/router";
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';

export type StatusFilter = 'all' | 'active' | 'inactive';
export type RoleFilter = 'all' | 'admin' | 'user';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, MatFormFieldModule, MatSelectModule],
  templateUrl: './sidebar.component.html',
})

export class SidebarComponent {

  @Output() statusFilterChange = new EventEmitter<StatusFilter>();
  @Output() roleFilterChange = new EventEmitter<RoleFilter>();

  selectedStatus: StatusFilter = 'all';
  selectedRole: RoleFilter = 'all';

  constructor() { }

  onFilterChange(): void {
    this.statusFilterChange.emit(this.selectedStatus);
    this.roleFilterChange.emit(this.selectedRole);
  }

  clearFilters(): void {
    this.selectedStatus = 'all';
    this.selectedRole = 'all';
    this.onFilterChange();
  }
}
