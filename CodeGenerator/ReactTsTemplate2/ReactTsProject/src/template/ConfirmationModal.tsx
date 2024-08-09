import React, { FC, ReactNode } from 'react';
import { Modal, Button } from 'react-bootstrap';

type Props = {
    show: boolean;
    title: ReactNode;
    body: ReactNode;
    buttonPositive: ReactNode;
    buttonNegative: ReactNode;
    disableButton?: boolean;
    onPositive: () => void;
    onNegative: () => void;
};

const ConfirmationModal: FC<Props> = ({
    show,
    title,
    body,
    buttonPositive,
    buttonNegative,
    onPositive,
    onNegative,
    disableButton,
}) => {
    return (
        <Modal size="sm" show={show} onHide={() => onNegative()} dialogClassName="modal-dialog-centered">
            <Modal.Header closeButton>
                <Modal.Title>{title}</Modal.Title>
            </Modal.Header>
            <Modal.Body>{body}</Modal.Body>
            <Modal.Footer>
                <Button variant="secondary" style={{ marginRight: 'auto' }} onClick={() => onNegative()} disabled={disableButton}>
                    {buttonNegative}
                </Button>
                <Button onClick={() => onPositive()} variant="primary" disabled={disableButton}>
                    {buttonPositive}
                </Button>
            </Modal.Footer>
        </Modal>
    );
};

export default ConfirmationModal;
