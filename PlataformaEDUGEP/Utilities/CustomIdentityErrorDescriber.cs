using Microsoft.AspNetCore.Identity;

namespace PlataformaEDUGEP.Utilities
{
    /// <summary>
    /// Provides custom error descriptions for identity-related errors.
    /// </summary>
    public class CustomIdentityErrorDescriber : IdentityErrorDescriber
    {
        /// <summary>
        /// Returns an <see cref="IdentityError"/> indicating a password validation failure
        /// due to the lack of a non-alphanumeric character.
        /// </summary>
        /// <returns>An <see cref="IdentityError"/> indicating the error.</returns>
        public override IdentityError PasswordRequiresNonAlphanumeric()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresNonAlphanumeric),
                Description = "As palavras-passe devem ter pelo menos um carácter não alfanumérico."
            };
        }

        /// <summary>
        /// Returns an <see cref="IdentityError"/> indicating a password validation failure
        /// due to the lack of an uppercase character.
        /// </summary>
        /// <returns>An <see cref="IdentityError"/> indicating the error.</returns>
        public override IdentityError PasswordRequiresUpper()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresUpper),
                Description = "As palavras-passe devem ter pelo menos uma letra maiúscula ('A'-'Z')."
            };
        }

        /// <summary>
        /// Returns an <see cref="IdentityError"/> indicating a password validation failure
        /// due to the lack of a digit.
        /// </summary>
        /// <returns>An <see cref="IdentityError"/> indicating the error.</returns>
        public override IdentityError PasswordRequiresDigit()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresDigit),
                Description = "As palavras-passe devem ter pelo menos um dígito ('0'-'9')."
            };
        }

        /// <summary>
        /// Returns an <see cref="IdentityError"/> indicating a password validation failure
        /// due to the lack of a lowercase character.
        /// </summary>
        /// <returns>An <see cref="IdentityError"/> indicating the error.</returns>
        public override IdentityError PasswordRequiresLower()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresLower),
                Description = "As palavras-passe devem ter pelo menos uma letra minúscula ('a'-'z')."
            };
        }

        /// <summary>
        /// Returns an <see cref="IdentityError"/> indicating a password validation failure
        /// due to its length not being long enough.
        /// </summary>
        /// <param name="length">The minimum length of the password.</param>
        /// <returns>An <see cref="IdentityError"/> indicating the error.</returns>
        public override IdentityError PasswordTooShort(int length)
        {
            return new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description = $"As palavras-passe devem ter pelo menos {length} carácteres."
            };
        }

        /// <summary>
        /// Returns an <see cref="IdentityError"/> indicating an invalid user name.
        /// </summary>
        /// <param name="userName">The invalid user name.</param>
        /// <returns>An <see cref="IdentityError"/> indicating the error.</returns>
        public override IdentityError InvalidUserName(string userName)
        {
            return new IdentityError
            {
                Code = nameof(InvalidUserName),
                Description = $"O nome de utilizador '{userName}' é inválido."
            };
        }

        /// <summary>
        /// Returns an <see cref="IdentityError"/> indicating a duplicate user name.
        /// </summary>
        /// <param name="userName">The duplicate user name.</param>
        /// <returns>An <see cref="IdentityError"/> indicating the error.</returns>
        public override IdentityError DuplicateUserName(string userName)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = $"O nome de utilizador '{userName}' já está em uso."
            };
        }

        /// <summary>
        /// Returns an <see cref="IdentityError"/> indicating a duplicate email.
        /// </summary>
        /// <param name="email">The duplicate email.</param>
        /// <returns>An <see cref="IdentityError"/> indicating the error.</returns>
        public override IdentityError DuplicateEmail(string email)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description = $"O email '{email}' já está em uso."
            };
        }

        /// <summary>
        /// Returns an <see cref="IdentityError"/> indicating an invalid email.
        /// </summary>
        /// <param name="email">The invalid email.</param>
        /// <returns>An <see cref="IdentityError"/> indicating the error.</returns>
        public override IdentityError InvalidEmail(string email)
        {
            return new IdentityError
            {
                Code = nameof(InvalidEmail),
                Description = $"O email '{email}' é inválido."
            };
        }

        /// <summary>
        /// Returns an <see cref="IdentityError"/> indicating a duplicate role name.
        /// </summary>
        /// <param name="role">The duplicate role name.</param>
        /// <returns>An <see cref="IdentityError"/> indicating the error.</returns>
        public override IdentityError DuplicateRoleName(string role)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateRoleName),
                Description = $"O nome de função '{role}' já está em uso."
            };
        }

        /// <summary>
        /// Returns an <see cref="IdentityError"/> indicating an invalid role name.
        /// </summary>
        /// <param name="role">The invalid role name.</param>
        /// <returns>An <see cref="IdentityError"/> indicating the error.</returns>
        public override IdentityError InvalidRoleName(string role)
        {
            return new IdentityError
            {
                Code = nameof(InvalidRoleName),
                Description = $"O nome de função '{role}' é inválido."
            };
        }
    }
}
