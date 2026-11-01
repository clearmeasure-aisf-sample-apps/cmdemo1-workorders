# Note 39

A vendor phone number cannot exceed 50 characters. Vendor.PhoneNumberMaxLength is 50. CreateVendorCommandValidator applies MaximumLength(Vendor.PhoneNumberMaxLength) to the phone number. When the phone number is not null or empty, it must match the pattern ^[0-9+\-.\(\) ]*$ and contain at least 7 digits; the message is "Phone number is not valid." VendorMap maps the phone column with HasMaxLength(Vendor.PhoneNumberMaxLength). The phone number is optional.
