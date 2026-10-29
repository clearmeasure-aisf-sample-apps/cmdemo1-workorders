# Note 38

A vendor email address cannot exceed 255 characters. Vendor.EmailAddressMaxLength is 255. CreateVendorCommandValidator applies MaximumLength(Vendor.EmailAddressMaxLength) to the email address, and applies EmailAddress() with the message "Email address is not valid." only when the address is not null or empty. VendorMap maps the email column with HasMaxLength(Vendor.EmailAddressMaxLength). The email address is optional.
