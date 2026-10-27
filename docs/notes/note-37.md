# Note 37

A vendor contact name cannot exceed 100 characters. Vendor.ContactNameMaxLength is 100. CreateVendorCommandValidator applies MaximumLength(Vendor.ContactNameMaxLength) to the contact name. VendorMap maps the contact-name column with HasMaxLength(Vendor.ContactNameMaxLength). The contact name is optional.
