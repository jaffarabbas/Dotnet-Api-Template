import { Component } from '@angular/core';
import { AuthService } from '../../../services/auth.service';
import { AuthStorageService } from '../../../services/auth-storage.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home',
  imports: [],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent {
  constructor(private authService: AuthService,
    private authStorage: AuthStorageService,
    private router: Router) { }

  logout() {
    this.authService.logout().subscribe({
      next: () => {
        this.authStorage.clearAuthData();
        this.router.navigate(['/login']);
      },
      error: (error) => {
        console.error('Logout failed', error);
      }
    });
  }
}
